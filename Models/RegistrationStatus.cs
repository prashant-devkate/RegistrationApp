namespace RegistrationApp.Models;

/// <summary>
/// Represents the status of a registration
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// Registration created, awaiting payment
    /// </summary>
    PaymentPending,

    /// <summary>
    /// Payment failed or was abandoned
    /// </summary>
    PaymentFailed,

    /// <summary>
    /// Payment confirmed, registration is complete
    /// </summary>
    Confirmed,

    /// <summary>
    /// Registration cancelled by user or admin
    /// </summary>
    Cancelled
}
