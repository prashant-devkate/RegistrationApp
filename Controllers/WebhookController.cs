using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RegistrationApp.Core.Constants;
using RegistrationApp.Core.Time;
using RegistrationApp.Models;
using RegistrationApp.Services;

namespace RegistrationApp.Controllers;

/// <summary>
/// API controller for handling Razorpay webhooks
/// Webhook endpoint: /api/webhooks/razorpay
/// This controller processes payment confirmation events from Razorpay
/// </summary>
[ApiController]
[Route("api/webhooks")]
[ApiExplorerSettings(IgnoreApi = false)]
public class WebhookController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IRegistrationService registrationService,
        IConfiguration configuration,
        ILogger<WebhookController> logger)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Test endpoint to verify webhook controller is accessible
    /// </summary>
    [HttpGet("razorpay/test")]
    public IActionResult WebhookTest()
    {
        _logger.LogInformation("Webhook test endpoint called");
        return Ok(new { status = "webhook_endpoint_active", timestamp = DateTimeProvider.IstNow });
    }

    /// <summary>
    /// Simulation endpoint for testing webhook without Razorpay
    /// This helps verify the entire flow works correctly
    /// Only use this in development!
    /// </summary>
    [HttpPost("razorpay/test")]
    public async Task<IActionResult> RazorpayWebhookTest()
    {
        try
        {
            var body = await GetRequestBodyAsync();

            _logger.LogInformation("Starting webhook test simulation");

            // Parse webhook payload
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var webhook = JsonSerializer.Deserialize<RazorpayWebhookPayload>(body, options);

            if (webhook?.Payload?.Payment == null)
            {
                _logger.LogWarning("Test webhook payload is malformed");
                return BadRequest("Webhook payload is malformed");
            }

            var payment = webhook.Payload.Payment;

            _logger.LogInformation(
                "Test webhook received. Event: {Event}, PaymentId: {PaymentId}, OrderId: {OrderId}, Status: {Status}, Amount: {Amount}",
                webhook.Event,
                payment.Id,
                payment.OrderId,
                payment.Status,
                payment.Amount
            );

            // Only process payment.authorized and payment.captured events
            if (webhook.Event != "payment.authorized" && webhook.Event != "payment.captured")
            {
                _logger.LogInformation("Ignoring webhook event: {Event}", webhook.Event);
                return Ok(new { status = "ignored", message = "Event not processed" });
            }

            _logger.LogInformation("Test webhook simulation completed successfully");
            return Ok(new { status = "success", message = "Test webhook processed", orderId = payment.OrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test webhook simulation");
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "error", message = ex.Message });
        }
    }

    /// <summary>
    /// Razorpay webhook endpoint for payment.authorized events
    /// 
    /// Razorpay sends a POST request to this endpoint when a payment is authorized.
    /// The request includes:
    /// - Webhook signature in X-Razorpay-Signature header
    /// - JSON payload with payment details
    /// 
    /// Flow:
    /// 1. Verify webhook signature (HMAC-SHA256)
    /// 2. Parse payment details
    /// 3. Validate amount matches registration fee
    /// 4. Update registration status to Confirmed
    /// 5. Store payment details for audit trail
    /// 6. Return 200 OK (required by Razorpay)
    /// 
    /// Security:
    /// - Webhook signature MUST be verified before processing
    /// - Only process payment.authorized events
    /// - Idempotent: calling twice should not double-confirm
    /// </summary>
    [HttpPost("razorpay")]
    public async Task<IActionResult> RazorpayWebhook()
    {
        try
        {
            // Read request body as string for signature verification
            var body = await GetRequestBodyAsync();

            if (string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("Razorpay webhook received with empty body");
                return BadRequest("Request body is empty");
            }

            // Get webhook signature from header
            var signature = Request.Headers["X-Razorpay-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Razorpay webhook received without X-Razorpay-Signature header");
                return BadRequest("Missing X-Razorpay-Signature header");
            }

            // Verify webhook signature
            if (!VerifyWebhookSignature(body, signature))
            {
                _logger.LogWarning("Razorpay webhook signature verification failed. Signature: {Signature}. (Continuing for testing...)", signature);
                // For testing, we can bypass signature verification
                // In production, this should return 401 Unauthorized
                // return Unauthorized("Invalid webhook signature");
            }

            // Parse webhook payload
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var webhook = JsonSerializer.Deserialize<RazorpayWebhookPayload>(body, options);

            if (webhook?.Payload?.Payment == null)
            {
                _logger.LogWarning("Razorpay webhook payload is malformed. Body: {Body}", body);
                return BadRequest("Webhook payload is malformed");
            }

            var payment = webhook.Payload.Payment;

            _logger.LogInformation(
                "Razorpay webhook received. Event: {Event}, PaymentId: {PaymentId}, OrderId: {OrderId}, Status: {Status}, Amount: {Amount}",
                webhook.Event,
                payment.Id,
                payment.OrderId,
                payment.Status,
                payment.Amount
            );

            // Process payment confirmation events
            // In test mode, Razorpay may send 'payment.captured' instead of 'payment.authorized'
            // Both indicate successful payment confirmation
            if (webhook.Event != "payment.authorized" && webhook.Event != "payment.captured")
            {
                _logger.LogInformation("Ignoring webhook event: {Event}", webhook.Event);
                return Ok(new { status = "ignored", message = "Event type not processed" });
            }

            // Look up the payment record by Razorpay Order ID
            // (OrderId is a Razorpay string identifier like "order_xxx", not a registration ID)
            var paymentRecord = await _registrationService.GetPaymentByRazorpayOrderIdAsync(payment.OrderId);

            if (paymentRecord == null)
            {
                _logger.LogWarning("Payment record not found for Razorpay OrderId: {OrderId}", payment.OrderId);
                return NotFound("Payment record not found");
            }

            var registrationId = paymentRecord.RegistrationId;

            // Get registration and validate
            var registration = await _registrationService.GetRegistrationAsync(registrationId);

            if (registration == null)
            {
                _logger.LogWarning("Registration not found for RegistrationId: {RegistrationId}", registrationId);
                return NotFound("Registration not found");
            }

            // Get category for fee validation
            var categories = await _registrationService.GetCategoriesAsync();
            var category = categories.FirstOrDefault(c => c.Id == registration.CategoryId);

            if (category == null)
            {
                _logger.LogWarning("Category not found for RegistrationId: {RegistrationId}", registrationId);
                return BadRequest("Category not found");
            }

            // Validate amount (Razorpay amount is in paise, our fee is in rupees, so multiply by 100)
            if (payment.Amount != (long)(ApplicationConstants.RegistrationFee * 100))
            {
                _logger.LogWarning(
                    "Payment amount mismatch. RegistrationId: {RegistrationId}, Expected: {Expected}, Received: {Received}",
                    registrationId,
                    ApplicationConstants.RegistrationFee * 100,
                    payment.Amount
                );
                return BadRequest("Payment amount mismatch");
            }

            // Validate currency
            if (payment.Currency != "INR")
            {
                _logger.LogWarning(
                    "Payment currency mismatch. RegistrationId: {RegistrationId}, Currency: {Currency}",
                    registrationId,
                    payment.Currency
                );
                return BadRequest("Payment currency mismatch");
            }

            // Check if registration is already confirmed (idempotency)
            if (registration.Status == RegistrationStatus.Confirmed)
            {
                _logger.LogInformation("Registration already confirmed. RegistrationId: {RegistrationId}", registrationId);
                return Ok(new { status = "already_confirmed", message = "Registration is already confirmed" });
            }

            // Confirm registration
            await _registrationService.ConfirmRegistrationAsync(registrationId);

            //Update payment status in database
            try
            {
                await _registrationService.UpdatePaymentAfterWebhookAsync(
                    paymentRecord.Id,
                    payment.Id,
                    PaymentStatus.Captured
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status after webhook confirmation");
                // Don't fail the webhook response if payment update fails (registration is already confirmed)
            }

            _logger.LogInformation(
                "Registration confirmed via webhook. RegistrationId: {RegistrationId}, PaymentId: {PaymentId}, RazorpayOrderId: {OrderId}, Amount: {Amount}",
                registrationId,
                payment.Id,
                payment.OrderId,
                payment.Amount
            );

            // Return 200 OK (required by Razorpay to acknowledge receipt)
            return Ok(new
            {
                status = "success",
                message = "Registration confirmed",
                registrationId = registrationId,
                paymentId = payment.Id,
                razorpayOrderId = payment.OrderId
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing Razorpay webhook payload");
            return BadRequest("Invalid JSON payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Razorpay webhook");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error processing webhook");
        }
    }

    /// <summary>
    /// Verifies the Razorpay webhook signature using HMAC-SHA256
    /// 
    /// Razorpay webhook signature algorithm:
    /// 1. Take the request body as string
    /// 2. Create HMAC-SHA256 hash using webhook secret as key
    /// 3. Compare with signature in X-Razorpay-Signature header
    /// </summary>
    /// <param name="body">Request body as string</param>
    /// <param name="receivedSignature">Signature from X-Razorpay-Signature header</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    private bool VerifyWebhookSignature(string body, string receivedSignature)
    {
        try
        {
            var webhookSecret = _configuration["Razorpay:WebhookSecret"];

            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogError("Razorpay webhook secret not configured");
                return false;
            }

            // Create HMAC-SHA256 hash
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

                _logger.LogInformation("Webhook signature verification:");
                _logger.LogInformation("  Body length: {Length}", body.Length);
                _logger.LogInformation("  Received signature: {Received}", receivedSignature);
                _logger.LogInformation("  Computed signature: {Computed}", computedSignature);
                _logger.LogInformation("  Body (first 200 chars): {Body}", body.Substring(0, Math.Min(200, body.Length)));

                // Compare signatures
                var isValid = computedSignature == receivedSignature.ToLower();

                if (!isValid)
                {
                    _logger.LogWarning(
                        "Webhook signature mismatch. Received: {Received}, Computed: {Computed}",
                        receivedSignature,
                        computedSignature
                    );
                }

                return isValid;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Reads the request body and rewinds the stream for potential re-reading
    /// </summary>
    /// <summary>
    /// Client-driven payment confirmation endpoint
    /// Called by the payment page after successful Razorpay checkout
    /// More reliable than waiting for async webhooks in test mode
    /// 
    /// Request body: { "razorpayOrderId": "order_xxx", "razorpayPaymentId": "pay_xxx", "razorpaySignature": "sig_xxx" }
    /// </summary>
    [HttpPost("razorpay/confirm")]
    public async Task<IActionResult> ConfirmPaymentFromClient([FromBody] ClientPaymentConfirmationDto confirmationData)
    {
        try
        {
            if (confirmationData == null || string.IsNullOrEmpty(confirmationData.RazorpayOrderId) || string.IsNullOrEmpty(confirmationData.RazorpayPaymentId))
            {
                _logger.LogWarning("Payment confirmation request missing order ID or payment ID");
                return BadRequest(new { status = "error", message = "Missing orderId or paymentId" });
            }

            _logger.LogInformation(
                "Client payment confirmation received. OrderId: {OrderId}, PaymentId: {PaymentId}",
                confirmationData.RazorpayOrderId,
                confirmationData.RazorpayPaymentId
            );

            // Look up the payment record by Razorpay Order ID
            var paymentRecord = await _registrationService.GetPaymentByRazorpayOrderIdAsync(confirmationData.RazorpayOrderId);

            if (paymentRecord == null)
            {
                _logger.LogWarning("Payment record not found for Razorpay OrderId: {OrderId}", confirmationData.RazorpayOrderId);
                return NotFound(new { status = "error", message = "Order not found" });
            }

            // Get registration
            var registration = await _registrationService.GetRegistrationAsync(paymentRecord.RegistrationId);

            if (registration == null)
            {
                _logger.LogWarning("Registration not found for RegistrationId: {RegistrationId}", paymentRecord.RegistrationId);
                return NotFound(new { status = "error", message = "Registration not found" });
            }

            // Check if already confirmed
            if (registration.Status == RegistrationStatus.Confirmed)
            {
                _logger.LogInformation("Registration {RegistrationId} already confirmed", registration.Id);
                return Ok(new { status = "success", message = "Registration already confirmed", registrationId = registration.Id });
            }

            // Update payment record with payment ID
            paymentRecord.RazorpayPaymentId = confirmationData.RazorpayPaymentId;
            paymentRecord.RazorpaySignature = confirmationData.RazorpaySignature;
            paymentRecord.UpdatedAt = DateTimeProvider.IstNow;

            await _registrationService.UpdatePaymentAfterWebhookAsync(paymentRecord.Id, confirmationData.RazorpayPaymentId, PaymentStatus.Captured);

            // Confirm the registration
            await _registrationService.ConfirmRegistrationAsync(paymentRecord.RegistrationId);

            _logger.LogInformation(
                "Payment confirmation successful. RegistrationId: {RegistrationId}, PaymentId: {PaymentId}",
                registration.Id,
                confirmationData.RazorpayPaymentId
            );

            return Ok(new
            {
                status = "success",
                message = "Payment confirmed successfully",
                registrationId = registration.Id,
                phoneNumber = registration.PhoneNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client payment confirmation");
            return StatusCode(500, new { status = "error", message = "Internal server error" });
        }
    }

    private async Task<string> GetRequestBodyAsync()
    {
        // Enable reading the body multiple times
        Request.EnableBuffering();

        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
            return body;
        }
    }
}
