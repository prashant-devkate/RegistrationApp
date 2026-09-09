using RegistrationApp.Core.Time;

namespace RegistrationApp.Models;

/// <summary>
/// Represents a payment attempt for a registration
/// A registration can have multiple payment records if payments fail
/// </summary>
public class Payment
{
    /// <summary>
    /// Unique identifier for the payment record
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID of the registration this payment is for
    /// </summary>
    public int RegistrationId { get; set; }

    /// <summary>
    /// Razorpay Order ID created for this payment
    /// Used to track the order in Razorpay system
    /// </summary>
    public string RazorpayOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Razorpay Payment ID returned by Razorpay after payment
    /// Only populated after successful payment
    /// </summary>
    public string? RazorpayPaymentId { get; set; }

    /// <summary>
    /// Amount to be paid in paise (smallest unit)
    /// e.g., 50000 = Rs 500.00
    /// </summary>
    public decimal AmountInPaise { get; set; }

    /// <summary>
    /// Currency code (e.g., "INR")
    /// </summary>
    public string Currency { get; set; } = "INR";

    /// <summary>
    /// Current status of this payment attempt
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Created;

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Razorpay Signature from webhook or payment response
    /// Used for verification
    /// </summary>
    public string? RazorpaySignature { get; set; }

    /// <summary>
    /// When this payment record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTimeProvider.IstNow;

    /// <summary>
    /// When this payment record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTimeProvider.IstNow;

    /// <summary>
    /// Idempotency key to prevent duplicate payment processing
    /// Ensures the same webhook event doesn't get processed twice
    /// </summary>
    public string? IdempotencyKey { get; set; }

    // Navigation property
    public Registration? Registration { get; set; }
}
