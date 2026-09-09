namespace RegistrationApp.Models;

/// <summary>
/// DTO for client-driven payment confirmation
/// Sent by the payment page after successful Razorpay checkout
/// </summary>
public class ClientPaymentConfirmationDto
{
    /// <summary>
    /// Razorpay Order ID (sent to checkout and returned with success)
    /// Example: "order_TZZyaDy0OeRW9p"
    /// </summary>
    public string? RazorpayOrderId { get; set; }

    /// <summary>
    /// Razorpay Payment ID (returned by checkout success handler)
    /// Example: "pay_IPhCM0YEu9r8UG"
    /// </summary>
    public string? RazorpayPaymentId { get; set; }

    /// <summary>
    /// Razorpay Signature (optional, for additional verification)
    /// Can be used to verify the payment authenticity
    /// </summary>
    public string? RazorpaySignature { get; set; }
}
