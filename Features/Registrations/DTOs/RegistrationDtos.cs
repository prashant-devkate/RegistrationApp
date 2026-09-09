using RegistrationApp.Models;

namespace RegistrationApp.Features.Registrations.DTOs;

/// <summary>
/// DTO for creating a new registration
/// </summary>
public class CreateRegistrationRequest
{
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
    /// Taluka where registrant is from
    /// </summary>
    public string Taluka { get; set; } = string.Empty;

    /// <summary>
    /// T-shirt size for the registrant
    /// </summary>
    public string TShirtSize { get; set; } = string.Empty;

    /// <summary>
    /// Category ID for this registration
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Name/path of the photo blob (optional)
    /// </summary>
    public string? PhotoBlobName { get; set; }

    /// <summary>
    /// Name/path of the Aadhar front image blob (optional)
    /// </summary>
    public string? AadharFrontBlobName { get; set; }

    /// <summary>
    /// Name/path of the Aadhar back image blob (optional)
    /// </summary>
    public string? AadharBackBlobName { get; set; }
}

/// <summary>
/// DTO for registration response
/// </summary>
public class RegistrationResponse
{
    /// <summary>
    /// Registration ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Full name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Category details
    /// </summary>
    public CategoryResponse? Category { get; set; }

    /// <summary>
    /// Photo blob URL
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// When the registration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the registration was confirmed
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }
}

/// <summary>
/// DTO for category response
/// </summary>
public class CategoryResponse
{
    /// <summary>
    /// Category ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Registration fee in paise
    /// </summary>
    public decimal RegistrationFeeInPaise { get; set; }

    /// <summary>
    /// Registration fee formatted for display (e.g., "Rs 500.00")
    /// </summary>
    public string FormattedFee { get; set; } = string.Empty;

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "INR";
}

/// <summary>
/// DTO for updating registration status
/// </summary>
public class UpdateRegistrationStatusRequest
{
    /// <summary>
    /// New status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// DTO for paginated registration list
/// </summary>
public class PaginatedRegistrationsResponse
{
    /// <summary>
    /// List of registrations
    /// </summary>
    public List<RegistrationResponse> Items { get; set; } = [];

    /// <summary>
    /// Total count of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
