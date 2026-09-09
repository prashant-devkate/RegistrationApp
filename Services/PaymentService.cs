using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RegistrationApp.Core.Constants;
using RegistrationApp.Core.Time;
using RegistrationApp.Data;
using RegistrationApp.Models;

namespace RegistrationApp.Services;

/// <summary>
/// DTO for order creation response from Razorpay
/// </summary>
internal class RazorpayOrderResponse
{
    public string? Id { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// DTO for Razorpay order creation response
/// </summary>
public class RazorpayOrderDto
{
    public string Id { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// DTO for payment verification from client
/// </summary>
public class PaymentVerificationDto
{
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

/// <summary>
/// Service for managing payments via Razorpay
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Create a Razorpay order for a registration
    /// </summary>
    Task<RazorpayOrderDto> CreateOrderAsync(int registrationId);

    /// <summary>
    /// Verify payment signature from Razorpay
    /// </summary>
    bool VerifyPaymentSignature(string orderId, string paymentId, string signature);

    /// <summary>
    /// Process payment after verification
    /// Idempotent - safe to call multiple times with same payment
    /// </summary>
    Task<Payment> ProcessPaymentAsync(int registrationId, string paymentId, string orderId, string signature);

    /// <summary>
    /// Handle Razorpay webhook event
    /// </summary>
    Task<bool> HandleWebhookAsync(string webhookBody, string webhookSignature);

    /// <summary>
    /// Get payment by registration ID
    /// </summary>
    Task<Payment?> GetLatestPaymentAsync(int registrationId);

    /// <summary>
    /// Get payment by Razorpay payment ID
    /// </summary>
    Task<Payment?> GetPaymentByRazorpayIdAsync(string razorpayPaymentId);

    /// <summary>
    /// Mark a payment as failed
    /// </summary>
    Task MarkPaymentFailedAsync(int registrationId, string errorMessage);
}

/// <summary>
/// Implementation of IPaymentService using Razorpay API
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly IRegistrationService _registrationService;
    private readonly HttpClient _httpClient;

    private string RazorpayKeyId => _configuration["Razorpay:KeyId"] ?? throw new InvalidOperationException("Razorpay KeyId not configured");
    private string RazorpayKeySecret => _configuration["Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay KeySecret not configured");
    private string RazorpayWebhookSecret => _configuration["Razorpay:WebhookSecret"] ?? throw new InvalidOperationException("Razorpay WebhookSecret not configured");

    public PaymentService(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        IRegistrationService registrationService,
        HttpClient httpClient)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Create a Razorpay order for a registration
    /// </summary>
    public async Task<RazorpayOrderDto> CreateOrderAsync(int registrationId)
    {
        try
        {
            var registration = await _registrationService.GetRegistrationAsync(registrationId);
            if (registration == null)
                throw new InvalidOperationException($"Registration not found: {registrationId}");

            if (registration.Category == null)
                throw new InvalidOperationException($"Category not found for registration {registrationId}");

            // Amount in paise (rupees * 100) using centralized fixed registration fee
            var amountInPaise = (long)(ApplicationConstants.RegistrationFee * 100);

            // Call Razorpay API to create order
            var orderResponse = await CreateRazorpayOrderAsync(registrationId, amountInPaise);

            // Create payment record in database
            var payment = new Payment
            {
                RegistrationId = registrationId,
                AmountInPaise = (decimal)amountInPaise,
                Currency = "INR",
                Status = PaymentStatus.Initiated,
                RazorpayOrderId = orderResponse.Id,
                CreatedAt = DateTimeProvider.IstNow,
                UpdatedAt = DateTimeProvider.IstNow
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Razorpay order created. Order ID: {OrderId}, Registration ID: {RegistrationId}, Amount: {Amount} paise",
                orderResponse.Id, registrationId, amountInPaise);

            return new RazorpayOrderDto
            {
                Id = orderResponse.Id,
                Amount = (int)amountInPaise,
                Currency = "INR",
                Status = orderResponse.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Razorpay order for registration {RegistrationId}: {ErrorMessage}",
                registrationId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Call Razorpay API to create an order
    /// </summary>
    private async Task<RazorpayOrderResponse> CreateRazorpayOrderAsync(int registrationId, long amountInPaise)
    {
        try
        {
            var url = "https://api.razorpay.com/v1/orders";

            _logger.LogInformation("Creating Razorpay order. Amount: {Amount} paise, Registration: {RegistrationId}", amountInPaise, registrationId);
            _logger.LogInformation("Using Razorpay KeyId: {KeyId}", RazorpayKeyId);

            // Prepare request body
            var requestBody = new
            {
                amount = amountInPaise,
                currency = "INR",
                receipt = registrationId.ToString(),
                notes = new
                {
                    registration_id = registrationId
                }
            };

            // Create request
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Add basic authentication (Key ID : Key Secret)
            var credentials = $"{RazorpayKeyId}:{RazorpayKeySecret}";
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _logger.LogInformation("Auth string created. Length: {Length}", authString.Length);

            request.Headers.Add("Authorization", $"Basic {authString}");

            // Serialize and add body
            var jsonContent = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to Razorpay API...");

            // Send request
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Razorpay API error. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Razorpay API error: {response.StatusCode}");
            }

            // Parse response
            var content = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(content))
            {
                var orderId = doc.RootElement.GetProperty("id").GetString();
                var status = doc.RootElement.GetProperty("status").GetString();

                return new RazorpayOrderResponse { Id = orderId, Status = status };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Razorpay API to create order");
            throw;
        }
    }

    /// <summary>
    /// Verify payment signature from Razorpay
    /// This ensures the payment response actually came from Razorpay
    /// </summary>
    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
    {
        try
        {
            // Create the string to sign
            var signData = $"{orderId}|{paymentId}";

            // Generate the signature using the key secret
            var secretBytes = Encoding.UTF8.GetBytes(RazorpayKeySecret);
            using (var hmac = new HMACSHA256(secretBytes))
            {
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signData));
                var calculatedSignature = Convert.ToHexString(signatureBytes).ToLowerInvariant();

                var isValid = calculatedSignature == signature.ToLowerInvariant();

                if (!isValid)
                {
                    _logger.LogWarning(
                        "Payment signature verification failed. Order: {OrderId}, Payment: {PaymentId}",
                        orderId, paymentId);
                }

                return isValid;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error verifying payment signature: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Process payment after verification
    /// This is idempotent - safe to call multiple times
    /// </summary>
    public async Task<Payment> ProcessPaymentAsync(int registrationId, string paymentId, string orderId, string signature)
    {
        try
        {
            // Verify signature first (security)
            if (!VerifyPaymentSignature(orderId, paymentId, signature))
            {
                _logger.LogWarning("Invalid payment signature for order {OrderId}", orderId);
                await MarkPaymentFailedAsync(registrationId, "Invalid payment signature");
                throw new InvalidOperationException("Payment signature verification failed");
            }

            // Check for existing processed payment (idempotency)
            var existingPayment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.RazorpayPaymentId == paymentId && 
                                         p.RegistrationId == registrationId);

            if (existingPayment != null && existingPayment.Status == PaymentStatus.Captured)
            {
                _logger.LogInformation("Payment already processed. Returning existing payment record. Payment ID: {PaymentId}",
                    paymentId);
                return existingPayment;
            }

            // Get the payment record by order ID
            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.RazorpayOrderId == orderId && 
                                         p.RegistrationId == registrationId);

            if (payment == null)
            {
                _logger.LogError("Payment record not found for order {OrderId}", orderId);
                throw new InvalidOperationException($"Payment record not found for order {orderId}");
            }

            // Update payment status
            payment.RazorpayPaymentId = paymentId;
            payment.RazorpaySignature = signature;
            payment.Status = PaymentStatus.Captured;
            payment.UpdatedAt = DateTimeProvider.IstNow;

            _dbContext.Payments.Update(payment);

            // Update registration status to Confirmed
            await _registrationService.ConfirmRegistrationAsync(registrationId);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment processed successfully. Payment ID: {PaymentId}, Order ID: {OrderId}, Registration ID: {RegistrationId}",
                paymentId, orderId, registrationId);

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing payment. Payment ID: {PaymentId}, Order ID: {OrderId}: {ErrorMessage}",
                paymentId, orderId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Handle Razorpay webhook events
    /// Webhooks are server-side notifications of payment status changes
    /// </summary>
    public async Task<bool> HandleWebhookAsync(string webhookBody, string webhookSignature)
    {
        try
        {
            // Verify webhook signature
            if (!VerifyWebhookSignature(webhookBody, webhookSignature))
            {
                _logger.LogWarning("Invalid webhook signature");
                return false;
            }

            // Parse webhook body
            using (var doc = JsonDocument.Parse(webhookBody))
            {
                var root = doc.RootElement;

                // Get event type
                if (!root.TryGetProperty("event", out var eventElement))
                {
                    _logger.LogWarning("Webhook missing event property");
                    return false;
                }

                var eventType = eventElement.GetString();

                // Process based on event type
                if (eventType == "payment.captured")
                {
                    return await HandlePaymentCapturedAsync(root);
                }
                else if (eventType == "payment.failed")
                {
                    return await HandlePaymentFailedAsync(root);
                }
                else
                {
                    _logger.LogInformation("Received unhandled webhook event: {EventType}", eventType);
                    return true; // Don't fail on unhandled events
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling webhook: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Get latest payment for a registration
    /// </summary>
    public async Task<Payment?> GetLatestPaymentAsync(int registrationId)
    {
        try
        {
            return await _dbContext.Payments
                .Where(p => p.RegistrationId == registrationId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving payment for registration {RegistrationId}: {ErrorMessage}",
                registrationId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get payment by Razorpay payment ID
    /// </summary>
    public async Task<Payment?> GetPaymentByRazorpayIdAsync(string razorpayPaymentId)
    {
        if (string.IsNullOrWhiteSpace(razorpayPaymentId))
            throw new ArgumentException("Razorpay payment ID cannot be empty", nameof(razorpayPaymentId));

        try
        {
            return await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.RazorpayPaymentId == razorpayPaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving payment by Razorpay ID: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Mark a payment as failed
    /// </summary>
    public async Task MarkPaymentFailedAsync(int registrationId, string errorMessage)
    {
        try
        {
            var payment = await GetLatestPaymentAsync(registrationId);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Failed;
                payment.ErrorMessage = errorMessage;
                payment.UpdatedAt = DateTimeProvider.IstNow;

                _dbContext.Payments.Update(payment);

                // Update registration status to PaymentFailed
                await _registrationService.UpdateRegistrationStatusAsync(registrationId, RegistrationStatus.PaymentFailed);

                await _dbContext.SaveChangesAsync();

                _logger.LogWarning("Payment marked as failed. Registration ID: {RegistrationId}, Error: {ErrorMessage}",
                    registrationId, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error marking payment as failed: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Verify webhook signature from Razorpay
    /// </summary>
    private bool VerifyWebhookSignature(string webhookBody, string webhookSignature)
    {
        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(RazorpayWebhookSecret);
            using (var hmac = new HMACSHA256(secretBytes))
            {
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(webhookBody));
                var calculatedSignature = Convert.ToHexString(signatureBytes).ToLowerInvariant();

                return calculatedSignature == webhookSignature.ToLowerInvariant();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error verifying webhook signature: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Handle payment.captured webhook event
    /// </summary>
    private async Task<bool> HandlePaymentCapturedAsync(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("payload", out var payload) ||
                !payload.TryGetProperty("payment", out var paymentData))
            {
                _logger.LogWarning("Invalid payment.captured webhook payload");
                return false;
            }

            if (!paymentData.TryGetProperty("entity", out var entity))
            {
                _logger.LogWarning("Payment entity missing in webhook");
                return false;
            }

            var paymentId = entity.GetProperty("id").GetString();
            var orderId = entity.GetProperty("order_id").GetString();

            if (string.IsNullOrEmpty(paymentId) || string.IsNullOrEmpty(orderId))
            {
                _logger.LogWarning("Missing payment or order ID in webhook");
                return false;
            }

            // Find the payment record by order ID
            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.RazorpayOrderId == orderId);

            if (payment == null)
            {
                _logger.LogWarning("Payment record not found for order {OrderId}", orderId);
                return false;
            }

            // Check idempotency - has this payment already been processed?
            var idempotencyKey = $"{orderId}|{paymentId}|captured";
            if (!string.IsNullOrEmpty(payment.IdempotencyKey) && payment.IdempotencyKey == idempotencyKey)
            {
                _logger.LogInformation("Duplicate webhook for payment {PaymentId} - already processed", paymentId);
                return true;
            }

            // Update payment status
            payment.RazorpayPaymentId = paymentId;
            payment.Status = PaymentStatus.Captured;
            payment.IdempotencyKey = idempotencyKey;
            payment.UpdatedAt = DateTimeProvider.IstNow;

            _dbContext.Payments.Update(payment);

            // Update registration status
            await _registrationService.ConfirmRegistrationAsync(payment.RegistrationId);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment captured webhook processed. Payment ID: {PaymentId}, Order ID: {OrderId}",
                paymentId, orderId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling payment.captured webhook: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Handle payment.failed webhook event
    /// </summary>
    private async Task<bool> HandlePaymentFailedAsync(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("payload", out var payload) ||
                !payload.TryGetProperty("payment", out var paymentData))
            {
                _logger.LogWarning("Invalid payment.failed webhook payload");
                return false;
            }

            if (!paymentData.TryGetProperty("entity", out var entity))
            {
                _logger.LogWarning("Payment entity missing in webhook");
                return false;
            }

            var orderId = entity.GetProperty("order_id").GetString();
            var description = entity.GetProperty("description").GetString();

            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogWarning("Missing order ID in payment.failed webhook");
                return false;
            }

            // Find the payment record
            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.RazorpayOrderId == orderId);

            if (payment == null)
            {
                _logger.LogWarning("Payment record not found for failed order {OrderId}", orderId);
                return false;
            }

            // Update payment status
            payment.Status = PaymentStatus.Failed;
            payment.ErrorMessage = description ?? "Payment failed";
            payment.UpdatedAt = DateTimeProvider.IstNow;

            _dbContext.Payments.Update(payment);

            // Update registration status
            await _registrationService.UpdateRegistrationStatusAsync(payment.RegistrationId, RegistrationStatus.PaymentFailed);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment failed webhook processed. Order ID: {OrderId}, Reason: {Reason}",
                orderId, description);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling payment.failed webhook: {ErrorMessage}", ex.Message);
            return false;
        }
    }
}
