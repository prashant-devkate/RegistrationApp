using RegistrationApp.Core.Time;

namespace RegistrationApp.Models;

/// <summary>
/// Represents a user registration
/// </summary>
public class Registration
{
    /// <summary>
    /// Unique identifier for the registration
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Full name of the registrant
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Address of the registrant
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the registrant
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Taluka (administrative division) where registrant is from
    /// </summary>
    public string Taluka { get; set; } = string.Empty;

    /// <summary>
    /// T-shirt size for the registrant (S, M, L, XL, XXL, XXXL)
    /// </summary>
    public string TShirtSize { get; set; } = string.Empty;

    /// <summary>
    /// Category ID for this registration
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Name/path of the uploaded photo in Azure Blob Storage
    /// e.g., "registrations/123/photo.jpg"
    /// </summary>
    public string? PhotoBlobName { get; set; }

    /// <summary>
    /// Name/path of the Aadhar card front image in Azure Blob Storage (optional)
    /// e.g., "registrations/123/aadhar_front.jpg"
    /// </summary>
    public string? AadharFrontBlobName { get; set; }

    /// <summary>
    /// Name/path of the Aadhar card back image in Azure Blob Storage (optional)
    /// e.g., "registrations/123/aadhar_back.jpg"
    /// </summary>
    public string? AadharBackBlobName { get; set; }

    /// <summary>
    /// Public URL of the uploaded photo in Azure Blob Storage
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Public URL of the Aadhar card front image in Azure Blob Storage
    /// </summary>
    public string? AadharFrontUrl { get; set; }

    /// <summary>
    /// Public URL of the Aadhar card back image in Azure Blob Storage
    /// </summary>
    public string? AadharBackUrl { get; set; }

    /// <summary>
    /// Current status of the registration
    /// </summary>
    public RegistrationStatus Status { get; set; } = RegistrationStatus.PaymentPending;

    /// <summary>
    /// When the registration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTimeProvider.IstNow;

    /// <summary>
    /// When the registration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTimeProvider.IstNow;

    /// <summary>
    /// When the registration was confirmed (payment completed)
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    // Navigation properties
    public Category? Category { get; set; }

    /// <summary>
    /// Collection of payment attempts for this registration
    /// A registration can have multiple payment attempts
    /// </summary>
    public ICollection<Payment> Payments { get; set; } = [];
}
