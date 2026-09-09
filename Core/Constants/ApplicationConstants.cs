namespace RegistrationApp.Core.Constants;

/// <summary>
/// Centralized application constants to prevent hardcoding
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// Application name
    /// </summary>
    public const string ApplicationName = "RegistrationApp";

    /// <summary>
    /// Application version
    /// </summary>
    public const string ApplicationVersion = "1.0.0";

    /// <summary>
    /// Default UTC timezone
    /// </summary>
    public const string DefaultTimeZone = "UTC";

    // ===== Validation Constants =====
    /// <summary>
    /// Maximum file size for photo uploads (10 MB)
    /// </summary>
    public const long MaxPhotoFileSizeInBytes = 10 * 1024 * 1024;

    /// <summary>
    /// How long a generated read-only SAS URL for a stored photo remains valid.
    /// </summary>
    public static readonly TimeSpan PhotoSasUrlLifetime = TimeSpan.FromHours(1);

    /// <summary>
    /// How long a SAS download link written into an exported Excel remains valid.
    /// Longer-lived so the exported file stays usable for a while after download.
    /// </summary>
    public static readonly TimeSpan ExportImageLinkLifetime = TimeSpan.FromDays(7);

    /// <summary>
    /// Allowed photo file extensions
    /// </summary>
    public static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    /// <summary>
    /// Maximum length for name field
    /// </summary>
    public const int MaxNameLength = 200;

    /// <summary>
    /// Maximum length for address field
    /// </summary>
    public const int MaxAddressLength = 500;

    /// <summary>
    /// Maximum length for email field
    /// </summary>
    public const int MaxEmailLength = 100;

    /// <summary>
    /// Maximum length for phone number field
    /// </summary>
    public const int MaxPhoneNumberLength = 20;

    // ===== Registration Fee Constants =====
    /// <summary>
    /// Fixed registration fee for all categories (in INR).
    /// Change this value to update the registration fee across the application.
    /// </summary>
    public const decimal RegistrationFee = 500m;

    // ===== Database Constants =====
    /// <summary>
    /// Default database command timeout in seconds
    /// </summary>
    public const int DefaultCommandTimeoutInSeconds = 30;

    /// <summary>
    /// Default database connection pool minimum size
    /// </summary>
    public const int DefaultMinPoolSize = 5;

    /// <summary>
    /// Default database connection pool maximum size for 100+ concurrent requests
    /// </summary>
    public const int DefaultMaxPoolSize = 100;

    // ===== Caching Constants =====
    /// <summary>
    /// Default cache duration for categories in seconds (30 minutes)
    /// </summary>
    public const int CategoriesCacheDurationInSeconds = 30 * 60;

    /// <summary>
    /// Default cache duration for registration in seconds (5 minutes)
    /// </summary>
    public const int RegistrationCacheDurationInSeconds = 5 * 60;

    /// <summary>
    /// Cache key prefix for categories
    /// </summary>
    public const string CacheCategoriesKey = "categories_all";

    /// <summary>
    /// Cache key prefix for registration
    /// </summary>
    public const string CacheRegistrationKeyPrefix = "registration_";

    // ===== Razorpay Constants =====
    /// <summary>
    /// Razorpay currency code
    /// </summary>
    public const string RazorpayCurrency = "INR";

    /// <summary>
    /// Razorpay order ID prefix for tracking
    /// </summary>
    public const string RazorpayOrderIdPrefix = "order_";

    /// <summary>
    /// Default request timeout for Razorpay API calls in seconds
    /// </summary>
    public const int RazorpayApiTimeoutInSeconds = 30;

    // ===== Azure Storage Constants =====
    /// <summary>
    /// Azure Blob Storage container name for registrations
    /// </summary>
    public const string AzureBlobContainerName = "registrations";

    /// <summary>
    /// Azure Blob Storage path prefix for registration photos
    /// </summary>
    public const string AzureBlobPhotoPathPrefix = "registrations";

    // ===== Pagination Constants =====
    /// <summary>
    /// Default page size for paginated queries
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Maximum page size to prevent abuse
    /// </summary>
    public const int MaxPageSize = 100;

    // ===== Logging Constants =====
    /// <summary>
    /// Correlation ID header name for distributed tracing
    /// </summary>
    public const string CorrelationIdHeaderName = "X-Correlation-ID";

    /// <summary>
    /// Request ID header name
    /// </summary>
    public const string RequestIdHeaderName = "X-Request-ID";
}