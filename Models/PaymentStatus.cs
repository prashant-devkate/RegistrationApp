namespace RegistrationApp.Models;

/// <summary>
/// Represents the status of a payment attempt
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Order created, awaiting payment
    /// </summary>
    Created,

    /// <summary>
    /// Payment initiated
    /// </summary>
    Initiated,

    /// <summary>
    /// Payment authorized but not captured
    /// </summary>
    Authorized,

    /// <summary>
    /// Payment successfully captured
    /// </summary>
    Captured,

    /// <summary>
    /// Payment failed
    /// </summary>
    Failed,

    /// <summary>
    /// Payment refunded
    /// </summary>
    Refunded
}
