using MediatR;

namespace RegistrationApp.Domain.Events;

/// <summary>
/// Base class for all domain events
/// Domain events represent significant things that happen in the domain
/// They are published after successful operations
/// </summary>
public abstract class DomainEvent : INotification
{
    /// <summary>
    /// Unique ID for this domain event
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// When this event occurred
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Event raised when a registration is created
/// </summary>
public class RegistrationCreatedEvent : DomainEvent
{
    public int RegistrationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}

/// <summary>
/// Event raised when a registration is confirmed
/// </summary>
public class RegistrationConfirmedEvent : DomainEvent
{
    public int RegistrationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; }
}

/// <summary>
/// Event raised when payment is processed successfully
/// </summary>
public class PaymentProcessedEvent : DomainEvent
{
    public int RegistrationId { get; set; }
    public int PaymentId { get; set; }
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public decimal AmountInPaise { get; set; }
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// Event raised when payment fails
/// </summary>
public class PaymentFailedEvent : DomainEvent
{
    public int RegistrationId { get; set; }
    public int PaymentId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Event raised when photo is uploaded
/// </summary>
public class PhotoUploadedEvent : DomainEvent
{
    public int RegistrationId { get; set; }
    public string PhotoBlobName { get; set; } = string.Empty;
}
