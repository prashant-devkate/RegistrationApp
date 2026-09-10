namespace RegistrationApp.Core.Constants;

/// <summary>
/// Hardcoded options for registration form dropdowns
/// </summary>
public static class RegistrationOptions
{
    /// <summary>
    /// Available Taluka (administrative division) options
    /// </summary>
    public static readonly string[] TalukaOptions = new[]
    {
        "Pathardi",
        "Shevgaon",
        "Shirur",
        "Ashti",
        "Other"
    };

    public const string OtherOption = "Other";

    /// <summary>
    /// Available T-shirt sizes
    /// </summary>
    public static readonly string[] TShirtSizes = new[]
    {
        "S",
        "M",
        "L",
        "XL",
        "XXL",
        "XXXL"
    };

    /// <summary>
    /// Get display name for T-shirt size
    /// </summary>
    public static string GetTShirtSizeDisplay(string size) => size switch
    {
        "S" => "Small",
        "M" => "Medium",
        "L" => "Large",
        "XL" => "Extra Large",
        "XXL" => "2X Large",
        "XXXL" => "3X Large",
        _ => size
    };

    /// <summary>
    /// Validate if provided size is valid
    /// </summary>
    public static bool IsValidTShirtSize(string? size) => !string.IsNullOrEmpty(size) && TShirtSizes.Contains(size);

    /// <summary>
    /// Validate if provided taluka is valid
    /// </summary>
    public static bool IsValidTaluka(string? taluka) => !string.IsNullOrEmpty(taluka) && TalukaOptions.Contains(taluka);
}
