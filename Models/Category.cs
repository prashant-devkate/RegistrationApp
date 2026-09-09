using RegistrationApp.Core.Time;

namespace RegistrationApp.Models;

/// <summary>
/// Represents a registration category
/// </summary>
public class Category
{
    public int Id { get; set; }

    /// <summary>
    /// Category name (e.g., "Student", "Professional", "Organization")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the category
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Registration fee in rupees
    /// e.g., 500 = Rs 500
    /// </summary>
    public decimal RegistrationFee { get; set; }

    /// <summary>
    /// Currency code (e.g., "INR")
    /// </summary>
    public string Currency { get; set; } = "INR";

    /// <summary>
    /// When this category was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTimeProvider.IstNow;

    // Navigation property
    public ICollection<Registration> Registrations { get; set; } = [];
}
