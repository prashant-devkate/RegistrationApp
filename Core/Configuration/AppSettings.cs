namespace RegistrationApp.Core.Configuration;

/// <summary>
/// Razorpay configuration settings
/// Loaded from appsettings.json with validation
/// </summary>
public class RazorpaySettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Razorpay";

    /// <summary>
    /// Razorpay Key ID for API authentication
    /// Never expose to browser
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Razorpay Key Secret for signing and verification
    /// Must be stored in Azure Key Vault in production
    /// </summary>
    public string KeySecret { get; set; } = string.Empty;

    /// <summary>
    /// Razorpay Webhook Secret for webhook signature verification
    /// Unique per webhook endpoint
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Razorpay API
    /// Default: https://api.razorpay.com/v1
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.razorpay.com/v1";

    /// <summary>
    /// Request timeout for Razorpay API calls in seconds
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 30;

    /// <summary>
    /// Enable test mode (uses test credentials)
    /// </summary>
    public bool IsTestMode { get; set; } = true;

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(KeyId) &&
               !string.IsNullOrWhiteSpace(KeySecret) &&
               !string.IsNullOrWhiteSpace(WebhookSecret);
    }
}

/// <summary>
/// Azure Storage configuration settings
/// </summary>
public class AzureStorageSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "AzureStorage";

    /// <summary>
    /// Connection string for Azure Blob Storage
    /// Can use managed identity in production instead
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Azure Storage account name (used with managed identity)
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Blob container name for registrations
    /// </summary>
    public string ContainerName { get; set; } = "registrations";

    /// <summary>
    /// Use managed identity instead of connection string
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public bool IsValid()
    {
        if (UseManagedIdentity)
        {
            return !string.IsNullOrWhiteSpace(AccountName) &&
                   !string.IsNullOrWhiteSpace(ContainerName);
        }

        return !string.IsNullOrWhiteSpace(ConnectionString) &&
               !string.IsNullOrWhiteSpace(ContainerName);
    }
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Connection string for SQL Server
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Minimum connection pool size
    /// Recommended: 5-10
    /// </summary>
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// Maximum connection pool size
    /// For 100+ concurrent requests: 100-150
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutInSeconds { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeoutInSeconds { get; set; } = 30;

    /// <summary>
    /// Enable query logging for debugging
    /// Warning: Can impact performance, only enable in development
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ConnectionString) &&
               MinPoolSize > 0 &&
               MaxPoolSize >= MinPoolSize;
    }
}

/// <summary>
/// Caching configuration settings
/// </summary>
public class CachingSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Caching";

    /// <summary>
    /// Enable distributed caching via Redis
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Redis connection string
    /// Format: hostname:port
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Cache expiration time in seconds for default items
    /// </summary>
    public int DefaultExpirationInSeconds { get; set; } = 1800; // 30 minutes

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ConnectionString) &&
               DefaultExpirationInSeconds > 0;
    }
}

/// <summary>
/// Application Insights configuration settings
/// </summary>
public class ApplicationInsightsSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "ApplicationInsights";

    /// <summary>
    /// Instrumentation key for Application Insights
    /// </summary>
    public string InstrumentationKey { get; set; } = string.Empty;

    /// <summary>
    /// Enable detailed diagnostics logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Sampling percentage (0-100)
    /// 100 = all requests sampled, 50 = 50% sampling
    /// </summary>
    public decimal SamplingPercentage { get; set; } = 100;

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(InstrumentationKey) &&
               SamplingPercentage >= 0 &&
               SamplingPercentage <= 100;
    }
}

/// <summary>
/// Main application settings container
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Environment name (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Razorpay settings
    /// </summary>
    public RazorpaySettings Razorpay { get; set; } = new();

    /// <summary>
    /// Azure Storage settings
    /// </summary>
    public AzureStorageSettings AzureStorage { get; set; } = new();

    /// <summary>
    /// Database settings
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Caching settings
    /// </summary>
    public CachingSettings Caching { get; set; } = new();

    /// <summary>
    /// Application Insights settings
    /// </summary>
    public ApplicationInsightsSettings ApplicationInsights { get; set; } = new();

    /// <summary>
    /// Validate all configurations
    /// </summary>
    public bool IsValid()
    {
        return Razorpay.IsValid() &&
               AzureStorage.IsValid() &&
               Database.IsValid();
    }
}
