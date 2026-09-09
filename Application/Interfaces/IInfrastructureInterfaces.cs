namespace RegistrationApp.Application.Interfaces;

/// <summary>
/// Caching abstraction for distributed and in-memory caching
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a value from cache
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a value in cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a value from cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove values matching a pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction for Azure Blob Storage operations
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload a file to blob storage
    /// </summary>
    Task<string> UploadAsync(int registrationId, Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file from blob storage
    /// </summary>
    Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from blob storage
    /// </summary>
    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the URL for a blob
    /// </summary>
    string GetUrl(string blobName);
}
