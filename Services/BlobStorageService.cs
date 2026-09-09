using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using RegistrationApp.Core.Constants;
using RegistrationApp.Core.Time;

namespace RegistrationApp.Services;

/// <summary>
/// Service for managing photo uploads and downloads from Azure Blob Storage
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload a photo to Azure Blob Storage
    /// </summary>
    /// <param name="registrationId">The registration ID</param>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">Original file name (used only to derive the file extension)</param>
    /// <param name="imageType">Logical image kind, e.g. "photo", "aadhar-front", "aadhar-back"</param>
    /// <returns>The blob name/path</returns>
    Task<string> UploadPhotoAsync(int registrationId, Stream fileStream, string fileName, string imageType);

    /// <summary>
    /// Download a photo from Azure Blob Storage
    /// </summary>
    /// <param name="blobName">The blob name/path</param>
    /// <returns>The file stream</returns>
    Task<Stream> DownloadPhotoAsync(string blobName);

    /// <summary>
    /// Delete a photo from Azure Blob Storage
    /// </summary>
    /// <param name="blobName">The blob name/path</param>
    Task DeletePhotoAsync(string blobName);

    /// <summary>
    /// Get a URL for accessing the photo
    /// </summary>
    /// <param name="blobName">The blob name/path</param>
    /// <returns>The blob URL</returns>
    string GetPhotoUrl(string blobName);

    /// <summary>
    /// Get a read-only download URL valid for the given lifetime.
    /// Suitable for links embedded in exported files.
    /// </summary>
    /// <param name="blobName">The blob name/path</param>
    /// <param name="lifetime">How long the link should remain valid</param>
    string GetDownloadUrl(string blobName, TimeSpan lifetime);
}

/// <summary>
/// Implementation of IBlobStorageService using Azure Blob Storage SDK
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _accountName;

    public BlobStorageService(
        BlobContainerClient containerClient,
        ILogger<BlobStorageService> logger)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _accountName = _containerClient.AccountName;
    }

    /// <summary>
    /// Upload a photo to Azure Blob Storage
    /// Stores photos with the structure: registrations/{registrationId}/{imageType}_{timestamp}.{ext}
    /// </summary>
    public async Task<string> UploadPhotoAsync(int registrationId, Stream fileStream, string fileName, string imageType)
    {
        if (fileStream == null)
            throw new ArgumentNullException(nameof(fileStream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        // Validate file size (centralized limit)
        if (fileStream.Length > ApplicationConstants.MaxPhotoFileSizeInBytes)
        {
            _logger.LogWarning("Photo upload attempted for registration {RegistrationId} exceeds max size. Size: {FileSize} bytes",
                registrationId, fileStream.Length);
            throw new InvalidOperationException(Messages.ErrorPhotoSizeTooLarge);
        }

        // Validate file extension
        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!validExtensions.Contains(extension))
        {
            _logger.LogWarning("Invalid file extension attempted: {Extension}", extension);
            throw new InvalidOperationException("Only image files (.jpg, .png, .gif, .webp) are allowed");
        }

        try
        {
            // Sanitize the blob name
            var blobName = SanitizeBlobName(registrationId, imageType, extension);

            // Set content type based on file extension
            var contentType = GetContentType(extension);

            // Reset stream position if possible
            if (fileStream.CanSeek)
            {
                fileStream.Seek(0, SeekOrigin.Begin);
            }

            // Upload the blob with content type
            var blobClient = _containerClient.GetBlobClient(blobName);
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions);

            _logger.LogInformation("Photo uploaded successfully for registration {RegistrationId}. Blob name: {BlobName}",
                registrationId, blobName);

            return blobName;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError("Azure Storage error uploading photo for registration {RegistrationId}: {ErrorMessage}",
                registrationId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error uploading photo for registration {RegistrationId}: {ErrorMessage}",
                registrationId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Download a photo from Azure Blob Storage
    /// </summary>
    public async Task<Stream> DownloadPhotoAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be empty", nameof(blobName));

        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var download = await blobClient.DownloadAsync();
            return download.Value.Content;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Photo not found in blob storage: {BlobName}", blobName);
            throw new FileNotFoundException($"Photo not found: {blobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error downloading photo {BlobName}: {ErrorMessage}", blobName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Delete a photo from Azure Blob Storage
    /// </summary>
    public async Task DeletePhotoAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be empty", nameof(blobName));

        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();

            _logger.LogInformation("Photo deleted successfully: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting photo {BlobName}: {ErrorMessage}", blobName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get a URL for accessing the photo.
    /// The container is kept private; a short-lived read-only SAS URL is returned so the
    /// image can be viewed securely without exposing the container to anonymous public access.
    /// Falls back to the plain blob URI if a SAS cannot be generated (e.g. Managed Identity auth).
    /// </summary>
    public string GetPhotoUrl(string blobName)
    {
        return GetDownloadUrl(blobName, ApplicationConstants.PhotoSasUrlLifetime);
    }

    /// <summary>
    /// Get a read-only download URL valid for the given lifetime.
    /// The container is kept private; a signed SAS URL is returned so the image can be
    /// accessed securely (e.g. from a link in an exported Excel) without anonymous access.
    /// Falls back to the plain blob URI if a SAS cannot be generated (e.g. Managed Identity auth).
    /// </summary>
    public string GetDownloadUrl(string blobName, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be empty", nameof(blobName));

        var blobClient = _containerClient.GetBlobClient(blobName);

        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(lifetime)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        _logger.LogWarning(
            "SAS URL could not be generated for blob {BlobName}; returning direct URI. " +
            "Ensure the storage client uses an account key or configure user-delegation SAS.",
            blobName);

        return blobClient.Uri.ToString();
    }

    /// <summary>
    /// Build a clean, valid, meaningful blob name.
    /// Produces a structure like: registrations/123/aadhar-front_20260909063839.jpg
    /// The original file name is intentionally ignored (only its extension is kept)
    /// so stored names are predictable, safe and descriptive.
    /// </summary>
    private static string SanitizeBlobName(int registrationId, string imageType, string extension)
    {
        // Normalize the logical image type into a safe, lowercase slug
        var slug = (imageType ?? string.Empty).Trim().ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = "image";

        // Normalize the extension
        var safeExtension = (extension ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(safeExtension) || !safeExtension.StartsWith('.'))
            safeExtension = ".jpg";

        var timestamp = DateTimeProvider.IstNow.ToString("yyyyMMddHHmmss");
        return $"registrations/{registrationId}/{slug}_{timestamp}{safeExtension}";
    }

    /// <summary>
    /// Get the content type based on file extension
    /// </summary>
    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}