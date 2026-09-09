namespace RegistrationApp.Features.Payments.DTOs;

/// <summary>
/// DTO for payment verification from client-side
/// Client sends payment ID, order ID, and signature for verification
/// </summary>
public class PaymentVerificationRequest
{
    /// <summary>
    /// Razorpay Payment ID returned by payment gateway
    /// </summary>
    public string RazorpayPaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Razorpay Order ID created by the server
    /// </summary>
    public string RazorpayOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Razorpay payment signature for verification
    /// Must be verified server-side to ensure payment came from Razorpay
    /// </summary>
    public string RazorpaySignature { get; set; } = string.Empty;
}

/// <summary>
/// DTO for payment response
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// Payment ID in our system
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Razorpay Order ID
    /// </summary>
    public string RazorpayOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Razorpay Payment ID (if payment completed)
    /// </summary>
    public string? RazorpayPaymentId { get; set; }

    /// <summary>
    /// Amount in paise
    /// </summary>
    public decimal AmountInPaise { get; set; }

    /// <summary>
    /// Amount formatted for display
    /// </summary>
    public string FormattedAmount { get; set; } = string.Empty;

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "INR";

    /// <summary>
    /// Payment status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the payment was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the payment was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for Razorpay order response
/// </summary>
public class RazorpayOrderResponse
{
    /// <summary>
    /// Razorpay Order ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Amount in paise
    /// </summary>
    public int AmountInPaise { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "INR";

    /// <summary>
    /// Public key for Razorpay Checkout
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Email for prefilling checkout
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Name for prefilling checkout
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Registration ID for tracking
    /// </summary>
    public int RegistrationId { get; set; }
}
