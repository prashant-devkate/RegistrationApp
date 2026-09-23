using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RegistrationApp.Core.Time;
using RegistrationApp.Data;
using RegistrationApp.Models;

namespace RegistrationApp.Services;

/// <summary>
/// Outcome of a reconciliation sweep. Distinguishes "nothing to do" from
/// "found work but could not resolve it", which otherwise look identical.
/// </summary>
public record ReconciliationSummary(int Examined, int Updated, int AlreadyCorrect, int NoAttempts, int Unresolved)
{
    public string Describe() => Examined == 0
        ? "No pending payments were in the reconciliation window."
        : $"Examined {Examined}, updated {Updated}, already correct {AlreadyCorrect}, never paid {NoAttempts}, unresolved {Unresolved}.";
}

/// <summary>
/// Outcome of the Razorpay-side captured-payment sweep.
/// </summary>
public record CapturedSweepSummary(
    int CapturedSeen,
    int AlreadyCorrect,
    int PaymentsUpdated,
    int RegistrationsConfirmed,
    int Unmatched,
    int AmountMismatched)
{
    public string Describe() =>
        $"Razorpay captured payments seen {CapturedSeen}, already correct {AlreadyCorrect}, payments corrected {PaymentsUpdated}, registrations confirmed {RegistrationsConfirmed}, unmatched {Unmatched}, amount mismatches {AmountMismatched}.";
}

/// <summary>
/// Translates Razorpay's payment status strings into <see cref="PaymentStatus"/>.
/// Shared by the webhook and the reconciliation job so both record identical,
/// source-derived statuses rather than assuming an outcome.
/// </summary>
public static class RazorpayStatusMapper
{
    /// <summary>
    /// Maps a Razorpay payment status string onto our enum.
    /// Returns null for unrecognised values so an unknown state never silently
    /// becomes a wrong status.
    /// </summary>
    public static PaymentStatus? Map(string? razorpayStatus) => razorpayStatus?.ToLowerInvariant() switch
    {
        "captured" => PaymentStatus.Captured,
        "authorized" => PaymentStatus.Authorized,
        "refunded" => PaymentStatus.Refunded,
        "failed" => PaymentStatus.Failed,
        "created" => PaymentStatus.Created,
        _ => null
    };

    /// <summary>
    /// Ranks Razorpay statuses so the most advanced attempt on an order wins
    /// </summary>
    public static int Rank(string? razorpayStatus) => razorpayStatus?.ToLowerInvariant() switch
    {
        "captured" => 5,
        "refunded" => 4,
        "authorized" => 3,
        "created" => 2,
        "failed" => 1,
        _ => 0
    };
}

/// <summary>
/// Configuration for the payment reconciliation job.
/// Bound from the "PaymentReconciliation" section of appsettings.
/// </summary>
public class PaymentReconciliationOptions
{
    public const string SectionName = "PaymentReconciliation";

    /// <summary>
    /// Whether the background reconciliation job runs
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often the job polls for unreconciled payments. Defaults to once a day.
    /// </summary>
    public int IntervalMinutes { get; set; } = 1440;

    /// <summary>
    /// Grace period before a pending payment is considered stale.
    /// Gives the webhook and the client-side confirmation a chance to arrive first.
    /// </summary>
    public int MinimumAgeMinutes { get; set; } = 15;

    /// <summary>
    /// How far back to look. Orders older than this are abandoned rather than reconciled.
    /// </summary>
    public int LookbackDays { get; set; } = 15;

    /// <summary>
    /// Maximum number of payments examined per run, to bound Razorpay API usage.
    /// Sized for the daily schedule so a burst of stuck payments clears in one run.
    /// </summary>
    public int BatchSize { get; set; } = 300;
}

/// <summary>
/// Reconciles payments that are still pending locally against Razorpay's records.
///
/// Webhook delivery is not guaranteed: the client may close the browser before the
/// confirmation call fires, and a rejected or dropped webhook leaves a payment stuck
/// in a non-terminal status even though Razorpay has captured the money. This service
/// closes that gap by asking Razorpay directly for the authoritative status.
/// </summary>
public interface IPaymentReconciliationService
{
    /// <summary>
    /// Reconciles all stale pending payments and reports what happened.
    /// </summary>
    Task<ReconciliationSummary> ReconcilePendingPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sweeps Razorpay's captured payments for the lookback window and corrects any
    /// local record that does not reflect them.
    ///
    /// This is the mirror image of <see cref="ReconcilePendingPaymentsAsync"/>. That pass
    /// starts from local pending rows and asks about their order, so it cannot see a
    /// payment made against a *different* order (a retry) or a local row whose status has
    /// already moved out of the pending set. Starting from Razorpay's own payment list
    /// closes both gaps, matching back via the registration id stored in the order notes.
    /// </summary>
    Task<CapturedSweepSummary> ReconcileCapturedPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconciles a single payment against Razorpay and returns the authoritative status
    /// reported by Razorpay, or null if Razorpay has no usable payment for the order.
    /// </summary>
    Task<PaymentStatus?> ReconcilePaymentAsync(int paymentId, string? razorpaySignature = null, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class PaymentReconciliationService : IPaymentReconciliationService
{
    private static readonly PaymentStatus[] PendingStatuses =
    {
        PaymentStatus.Created,
        PaymentStatus.Initiated,
        PaymentStatus.Authorized
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IRegistrationService _registrationService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly PaymentReconciliationOptions _options;
    private readonly ILogger<PaymentReconciliationService> _logger;

    private string RazorpayKeyId => _configuration["Razorpay:KeyId"] ?? throw new InvalidOperationException("Razorpay KeyId not configured");
    private string RazorpayKeySecret => _configuration["Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay KeySecret not configured");

    public PaymentReconciliationService(
        ApplicationDbContext dbContext,
        IRegistrationService registrationService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        Microsoft.Extensions.Options.IOptions<PaymentReconciliationOptions> options,
        ILogger<PaymentReconciliationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClientFactory?.CreateClient(nameof(PaymentReconciliationService)) ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ReconciliationSummary> ReconcilePendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeProvider.IstNow;
        var staleBefore = now.AddMinutes(-_options.MinimumAgeMinutes);
        var lookbackAfter = now.AddDays(-_options.LookbackDays);

        var candidates = await _dbContext.Payments
            .Where(p => PendingStatuses.Contains(p.Status)
&& p.RazorpayOrderId != string.Empty
&& p.CreatedAt < staleBefore
&& p.CreatedAt >= lookbackAfter)
            .OrderBy(p => p.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            // Logged explicitly: "nothing found" and "found nothing to fix" are different
            // problems, and the window itself is the usual reason for the former.
            var totalPending = await _dbContext.Payments
                .CountAsync(p => PendingStatuses.Contains(p.Status), cancellationToken);

            _logger.LogInformation(
                "Payment reconciliation found no candidates. Pending payments in DB (any age): {TotalPending}. Window: created before {StaleBefore} and after {LookbackAfter}.",
                totalPending, staleBefore, lookbackAfter);

            return new ReconciliationSummary(0, 0, 0, 0, 0);
        }

        _logger.LogInformation("Payment reconciliation starting for {Count} pending payment(s)", candidates.Count);

        var updated = 0;
        var alreadyCorrect = 0;
        var noAttempts = 0;
        var unresolved = 0;

        foreach (var payment in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var previousStatus = payment.Status;
                var (resolvedStatus, hadAttempts) = await ReconcileAsync(payment, null, cancellationToken);

                if (!hadAttempts)
                {
                    // Order was never paid. Correct and final, not a failure.
                    noAttempts++;
                }
                else if (!resolvedStatus.HasValue)
                {
                    unresolved++;
                }
                else if (resolvedStatus.Value != previousStatus)
                {
                    updated++;
                }
                else
                {
                    alreadyCorrect++;
                }
            }
            catch (Exception ex)
            {
                unresolved++;

                // One bad record must not stop the rest of the batch
                _logger.LogError(ex, "Error reconciling PaymentId: {PaymentId}, OrderId: {OrderId}",
                    payment.Id, payment.RazorpayOrderId);
            }
        }

        var summary = new ReconciliationSummary(candidates.Count, updated, alreadyCorrect, noAttempts, unresolved);

        _logger.LogInformation("Payment reconciliation finished. {Summary}", summary.Describe());

        return summary;
    }

    /// <inheritdoc />
    public async Task<PaymentStatus?> ReconcilePaymentAsync(int paymentId, string? razorpaySignature = null, CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments.FindAsync(new object[] { paymentId }, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for reconciliation. PaymentId: {PaymentId}", paymentId);
            return null;
        }

        return (await ReconcileAsync(payment, razorpaySignature, cancellationToken)).Status;
    }

    /// <summary>
    /// Resolves one payment against Razorpay. <c>HadAttempts</c> is false when Razorpay
    /// holds no payment attempts for the order, which means the checkout was abandoned
    /// and there is nothing to correct — distinct from a failure to resolve.
    /// </summary>
    private async Task<(PaymentStatus? Status, bool HadAttempts)> ReconcileAsync(Payment payment, string? razorpaySignature, CancellationToken cancellationToken)
    {
        var razorpayPayments = await FetchOrderPaymentsAsync(payment.RazorpayOrderId, cancellationToken);

        if (razorpayPayments.Count == 0)
        {
            // Razorpay returned the order but no payment attempts against it. This is an
            // abandoned checkout, not a discrepancy, so there is nothing to correct.
            _logger.LogInformation(
                "Razorpay reported no payment attempts for OrderId: {OrderId} (PaymentId: {PaymentId}, local status {Status}). Nothing to reconcile.",
                payment.RazorpayOrderId, payment.Id, payment.Status);
            return (null, false);
        }

        _logger.LogInformation(
            "Razorpay returned {Count} attempt(s) for OrderId: {OrderId}. Statuses: {Statuses}",
            razorpayPayments.Count,
            payment.RazorpayOrderId,
            string.Join(", ", razorpayPayments.Select(p => p.Status)));

        // Razorpay allows multiple attempts against one order; the most advanced attempt
        // is the authoritative one for the order as a whole.
        var match = razorpayPayments
            .OrderByDescending(p => RazorpayStatusMapper.Rank(p.Status))
            .First();

        var mappedStatus = RazorpayStatusMapper.Map(match.Status);

        if (mappedStatus == null)
        {
            _logger.LogWarning(
                "Unrecognised Razorpay payment status '{Status}' for OrderId: {OrderId}. Leaving local status unchanged.",
                match.Status, payment.RazorpayOrderId);
            return (null, true);
        }

        // Only money-bearing states require the amount to agree. Refusing on a mismatch
        // means we never record a status derived from an unrelated amount.
        if (mappedStatus is PaymentStatus.Captured or PaymentStatus.Authorized
&& match.Amount != (long)payment.AmountInPaise)
        {
            _logger.LogWarning(
                "Amount mismatch during reconciliation. PaymentId: {PaymentId}, OrderId: {OrderId}, Expected: {Expected}, Razorpay: {Received}",
                payment.Id, payment.RazorpayOrderId, (long)payment.AmountInPaise, match.Amount);
            return (null, true);
        }

        if (payment.Status == mappedStatus.Value)
        {
            return (mappedStatus, true);
        }

        var previousStatus = payment.Status;

        await _registrationService.UpdatePaymentAfterWebhookAsync(payment.Id, match.Id, mappedStatus.Value, razorpaySignature);

        _logger.LogWarning(
            "Payment status corrected from Razorpay. PaymentId: {PaymentId}, OrderId: {OrderId}, RazorpayPaymentId: {RazorpayPaymentId}, Local: {Local}, Razorpay: {Remote}",
            payment.Id, payment.RazorpayOrderId, match.Id, previousStatus, mappedStatus.Value);

        if (mappedStatus.Value != PaymentStatus.Captured)
        {
            return (mappedStatus, true);
        }

        var registration = await _registrationService.GetRegistrationAsync(payment.RegistrationId);

        if (registration != null && registration.Status != RegistrationStatus.Confirmed)
        {
            await _registrationService.ConfirmRegistrationAsync(payment.RegistrationId);

            _logger.LogWarning(
                "Registration confirmed via reconciliation. RegistrationId: {RegistrationId}, PaymentId: {PaymentId}",
                payment.RegistrationId, payment.Id);
        }

        return (mappedStatus, true);
    }

    /// <inheritdoc />
    public async Task<CapturedSweepSummary> ReconcileCapturedPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeProvider.IstNow;
        var from = now.AddDays(-_options.LookbackDays);

        var capturedSeen = 0;
        var alreadyCorrect = 0;
        var paymentsUpdated = 0;
        var registrationsConfirmed = 0;
        var unmatched = 0;
        var amountMismatched = 0;

        await foreach (var rp in FetchCapturedPaymentsAsync(from, now, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Only money-in-hand states are ever acted on here. Anything else is left to
            // the order-driven pass, so this sweep can never mark a payment wrongly.
            if (!string.Equals(rp.Status, "captured", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            capturedSeen++;

            if (rp.RegistrationId == null)
            {
                unmatched++;
                _logger.LogWarning(
                    "Captured Razorpay payment has no registration_id note. RazorpayPaymentId: {RazorpayPaymentId}, OrderId: {OrderId}",
                    rp.Id, rp.OrderId);
                continue;
            }

            // Matched by registration rather than order id, which is what catches a
            // retry paid against a second order.
            var payment = await _dbContext.Payments
                .Where(p => p.RegistrationId == rp.RegistrationId.Value)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (payment == null)
            {
                unmatched++;
                _logger.LogWarning(
                    "No local payment for captured Razorpay payment. RazorpayPaymentId: {RazorpayPaymentId}, RegistrationId: {RegistrationId}",
                    rp.Id, rp.RegistrationId);
                continue;
            }

            if (rp.Amount != (long)payment.AmountInPaise)
            {
                amountMismatched++;
                _logger.LogWarning(
                    "Amount mismatch on captured sweep. PaymentId: {PaymentId}, RegistrationId: {RegistrationId}, Expected: {Expected}, Razorpay: {Received}",
                    payment.Id, rp.RegistrationId, (long)payment.AmountInPaise, rp.Amount);
                continue;
            }

            var previousStatus = payment.Status;
            var paymentWasCorrect = previousStatus == PaymentStatus.Captured;

            if (!paymentWasCorrect)
            {
                // Status only. The original RazorpayOrderId is left untouched so the local
                // record still shows the order the checkout was started against.
                await _registrationService.UpdatePaymentAfterWebhookAsync(
                    payment.Id, rp.Id, PaymentStatus.Captured);

                paymentsUpdated++;

                _logger.LogWarning(
                    "Payment marked captured from Razorpay sweep. PaymentId: {PaymentId}, RegistrationId: {RegistrationId}, RazorpayPaymentId: {RazorpayPaymentId}, Previous: {Previous}",
                    payment.Id, rp.RegistrationId, rp.Id, previousStatus);
            }

            var registration = await _registrationService.GetRegistrationAsync(rp.RegistrationId.Value);

            if (registration != null && registration.Status != RegistrationStatus.Confirmed)
            {
                await _registrationService.ConfirmRegistrationAsync(rp.RegistrationId.Value);
                registrationsConfirmed++;

                _logger.LogWarning(
                    "Registration confirmed from Razorpay sweep. RegistrationId: {RegistrationId}, PaymentId: {PaymentId}",
                    rp.RegistrationId, payment.Id);
            }
            else if (paymentWasCorrect)
            {
                alreadyCorrect++;
            }
        }

        var summary = new CapturedSweepSummary(
            capturedSeen, alreadyCorrect, paymentsUpdated, registrationsConfirmed, unmatched, amountMismatched);

        _logger.LogInformation("Captured payment sweep finished. {Summary}", summary.Describe());

        return summary;
    }

    /// <summary>
    /// Streams Razorpay's payments for a window, following the API's skip/count pagination.
    /// </summary>
    private async IAsyncEnumerable<RazorpaySweptPayment> FetchCapturedPaymentsAsync(
        DateTime from,
        DateTime to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int pageSize = 100;

        var fromEpoch = new DateTimeOffset(from.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeSeconds();
        var toEpoch = new DateTimeOffset(to.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeSeconds();

        var skip = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var url = $"https://api.razorpay.com/v1/payments?from={fromEpoch}&to={toEpoch}&count={pageSize}&skip={skip}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            var credentials = $"{RazorpayKeyId}:{RazorpayKeySecret}";
            request.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials))}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Razorpay payments list error. Status: {StatusCode}, Skip: {Skip}, Response: {Response}",
                    response.StatusCode, skip, errorContent);
                yield break;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var page = new List<RazorpaySweptPayment>();

            using (var doc = JsonDocument.Parse(content))
            {
                if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Razorpay payments list had no 'items' array. Skip: {Skip}, Body: {Body}", skip, content);
                    yield break;
                }

                foreach (var item in items.EnumerateArray())
                {
                    page.Add(new RazorpaySweptPayment
                    {
                        Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                        OrderId = item.TryGetProperty("order_id", out var orderId) ? orderId.GetString() ?? string.Empty : string.Empty,
                        Status = item.TryGetProperty("status", out var status) ? status.GetString() ?? string.Empty : string.Empty,
                        Amount = item.TryGetProperty("amount", out var amount) ? amount.GetInt64() : 0,
                        RegistrationId = ReadRegistrationId(item)
                    });
                }
            }

            foreach (var payment in page)
            {
                yield return payment;
            }

            if (page.Count < pageSize)
            {
                yield break;
            }

            skip += pageSize;
        }
    }

    /// <summary>
    /// Reads the registration id from the order notes written at order creation.
    /// Razorpay may return note values as strings, so both forms are accepted.
    /// </summary>
    private static int? ReadRegistrationId(JsonElement item)
    {
        if (!item.TryGetProperty("notes", out var notes) || notes.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!notes.TryGetProperty("registration_id", out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private sealed class RazorpaySweptPayment
    {
        public string Id { get; init; } = string.Empty;
        public string OrderId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public long Amount { get; init; }
        public int? RegistrationId { get; init; }
    }

    /// <summary>
    /// Calls GET /v1/orders/{orderId}/payments to retrieve the authoritative payment
    /// list for an order.
    /// </summary>
    private async Task<List<RazorpayReconciledPayment>> FetchOrderPaymentsAsync(string orderId, CancellationToken cancellationToken)
    {
        var url = $"https://api.razorpay.com/v1/orders/{orderId}/payments";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        var credentials = $"{RazorpayKeyId}:{RazorpayKeySecret}";
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        request.Headers.Add("Authorization", $"Basic {authString}");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Razorpay reconciliation API error. OrderId: {OrderId}, Status: {StatusCode}, Response: {Response}",
                orderId, response.StatusCode, errorContent);
            return new List<RazorpayReconciledPayment>();
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var results = new List<RazorpayReconciledPayment>();

        using var doc = JsonDocument.Parse(content);

        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning(
                "Razorpay reconciliation response had no 'items' array. OrderId: {OrderId}, Body: {Body}",
                orderId, content);
            return results;
        }

        foreach (var item in items.EnumerateArray())
        {
            results.Add(new RazorpayReconciledPayment
            {
                Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                Status = item.TryGetProperty("status", out var status) ? status.GetString() ?? string.Empty : string.Empty,
                Amount = item.TryGetProperty("amount", out var amount) ? amount.GetInt64() : 0
            });
        }

        return results;
    }

    private sealed class RazorpayReconciledPayment
    {
        public string Id { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public long Amount { get; init; }
    }
}

/// <summary>
/// Periodically requests a reconciliation run so that payments captured by Razorpay
/// are never left pending because of a missed webhook. The run itself is performed by
/// <see cref="PaymentReconciliationWorker"/>, so the schedule and the dashboard button
/// share a single execution path.
/// </summary>
public class PaymentReconciliationBackgroundService : BackgroundService
{
    private readonly PaymentReconciliationRunner _runner;
    private readonly PaymentReconciliationOptions _options;
    private readonly ILogger<PaymentReconciliationBackgroundService> _logger;

    public PaymentReconciliationBackgroundService(
        PaymentReconciliationRunner runner,
        Microsoft.Extensions.Options.IOptions<PaymentReconciliationOptions> options,
        ILogger<PaymentReconciliationBackgroundService> logger)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Payment reconciliation job is disabled");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));

        _logger.LogInformation("Payment reconciliation schedule started. Interval: {Interval}", interval);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_runner.TryEnqueue("Scheduled"))
            {
                _logger.LogInformation("Scheduled reconciliation skipped; a run is already queued or in progress");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Payment reconciliation schedule stopped");
    }
}

/// <summary>
/// Performs reconciliation runs requested via <see cref="PaymentReconciliationRunner"/>.
///
/// Running here rather than inside the HTTP request means a run is owned by the host:
/// it cannot be cancelled by an admin navigating away from the dashboard, and it gets
/// its own DI scope rather than borrowing the request's.
/// </summary>
public class PaymentReconciliationWorker : BackgroundService
{
    private readonly PaymentReconciliationRunner _runner;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentReconciliationWorker> _logger;

    public PaymentReconciliationWorker(
        PaymentReconciliationRunner runner,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentReconciliationWorker> logger)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment reconciliation worker started");

        try
        {
            await foreach (var trigger in _runner.Queue.ReadAllAsync(stoppingToken))
            {
                await RunOnceAsync(trigger, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }

        _logger.LogInformation("Payment reconciliation worker stopped");
    }

    private async Task RunOnceAsync(string trigger, CancellationToken stoppingToken)
    {
        _runner.MarkStarted(trigger);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reconciliationService = scope.ServiceProvider.GetRequiredService<IPaymentReconciliationService>();

            var result = await reconciliationService.ReconcilePendingPaymentsAsync(stoppingToken);

            // Second, Razorpay-driven pass. Catches captured payments the order-driven
            // pass cannot see: retries paid against a different order, and rows whose
            // local status has already moved out of the pending set.
            var sweep = await reconciliationService.ReconcileCapturedPaymentsAsync(stoppingToken);

            var corrected = result.Updated + sweep.PaymentsUpdated + sweep.RegistrationsConfirmed;

            var message = corrected == 0
                ? $"No corrections needed. Checked {result.Examined} pending payment(s) and {sweep.CapturedSeen} captured Razorpay payment(s)."
                : $"Corrected {corrected} record(s): {result.Updated + sweep.PaymentsUpdated} payment(s) updated, {sweep.RegistrationsConfirmed} registration(s) confirmed. Checked {result.Examined} pending and {sweep.CapturedSeen} captured.";

            _runner.MarkCompleted(DateTimeProvider.IstNow, message, failed: false);

            _logger.LogInformation("Reconciliation run finished ({Trigger}). {Message}", trigger, message);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _runner.MarkCompleted(DateTimeProvider.IstNow, "Run was interrupted by application shutdown.", failed: true);
            throw;
        }
        catch (Exception ex)
        {
            // A failed run must never terminate the worker
            _logger.LogError(ex, "Unhandled error during payment reconciliation run ({Trigger})", trigger);
            _runner.MarkCompleted(DateTimeProvider.IstNow, "Run failed. Check the logs for details.", failed: true);
        }
    }
}