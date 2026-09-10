using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RegistrationApp.Core.Exceptions;
using RegistrationApp.Core.Time;
using RegistrationApp.Data;
using RegistrationApp.Models;

namespace RegistrationApp.Services;

/// <summary>
/// DTO for creating a new registration
/// </summary>
public class CreateRegistrationDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Taluka { get; set; } = string.Empty;
    public string TShirtSize { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string? PhotoBlobName { get; set; }
    public string? AadharFrontBlobName { get; set; }
    public string? AadharBackBlobName { get; set; }
}

/// <summary>
/// Service for managing user registrations
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Create a new registration with PaymentPending status
    /// </summary>
    Task<Registration> CreateRegistrationAsync(CreateRegistrationDto dto);

    /// <summary>
    /// Get a registration by ID
    /// </summary>
    Task<Registration?> GetRegistrationAsync(int id);

    /// <summary>
    /// Update the photo blob name for a registration
    /// </summary>
    Task UpdatePhotoBlobAsync(int registrationId, string blobName);

    /// <summary>
    /// Update image blob names and URLs for a registration (photo + aadhar images)
    /// </summary>
    Task UpdateImagesAsync(
        int registrationId,
        string? photoBlobName, string? photoUrl,
        string? aadharFrontBlobName, string? aadharFrontUrl,
        string? aadharBackBlobName, string? aadharBackUrl);

    /// <summary>
    /// Update registration status
    /// </summary>
    Task UpdateRegistrationStatusAsync(int registrationId, RegistrationStatus status);

    /// <summary>
    /// Confirm a registration after successful payment
    /// </summary>
    Task ConfirmRegistrationAsync(int registrationId);

    /// <summary>
    /// Get payment by Razorpay Order ID
    /// </summary>
    Task<Payment?> GetPaymentByRazorpayOrderIdAsync(string razorpayOrderId);

    /// <summary>
    /// Update payment status and Razorpay payment ID after webhook confirmation
    /// </summary>
    Task UpdatePaymentAfterWebhookAsync(int paymentId, string razorpayPaymentId, PaymentStatus status);

    /// <summary>
    /// Get all registrations (with optional filtering)
    /// </summary>
    Task<List<Registration>> GetAllRegistrationsAsync(RegistrationStatus? status = null);

    /// <summary>
    /// Get categories for dropdown
    /// </summary>
    Task<List<Category>> GetCategoriesAsync();
}

/// <summary>
/// Implementation of IRegistrationService
/// </summary>
public class RegistrationService : IRegistrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        ApplicationDbContext dbContext,
        ILogger<RegistrationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new registration with PaymentPending status
    /// Validates all input and ensures no duplicate email registrations
    /// </summary>
    public async Task<Registration> CreateRegistrationAsync(CreateRegistrationDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Validate input
        ValidateRegistrationInput(dto);

        try { 
        // Verify category exists
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId);
        if (category == null)
        {
            _logger.LogWarning("Invalid category ID: {CategoryId}", dto.CategoryId);
            throw new InvalidOperationException("Invalid category selected");
        }

        var normalizedPhone = dto.PhoneNumber.Trim();
        var duplicate = await _dbContext.Registrations
            .AnyAsync(r => r.PhoneNumber == normalizedPhone);

        if (duplicate)
        {
            _logger.LogWarning("Duplicate registration attempt for phone {PhoneNumber}", normalizedPhone);
            throw new DuplicateRegistrationException(
                $"A registration with phone number {normalizedPhone} already exists.");
        }

            var registration = new Registration
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                Taluka = dto.Taluka.Trim(),
                TShirtSize = dto.TShirtSize.Trim(),
                CategoryId = dto.CategoryId,
                PhotoBlobName = dto.PhotoBlobName,
                AadharFrontBlobName = dto.AadharFrontBlobName,
                AadharBackBlobName = dto.AadharBackBlobName,
                Status = RegistrationStatus.PaymentPending,
                CreatedAt = DateTimeProvider.IstNow,
                UpdatedAt = DateTimeProvider.IstNow
            };

            _dbContext.Registrations.Add(registration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Registration created successfully. Registration ID: {RegistrationId}, Phone: {PhoneNumber}",
                registration.Id, registration.PhoneNumber);

            return registration;
        }
        catch (DuplicateRegistrationException)
        {
            throw;  
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating registration for phone {PhoneNumber}: {ErrorMessage}",
                dto.PhoneNumber, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get a registration by ID with related data
    /// </summary>
    public async Task<Registration?> GetRegistrationAsync(int id)
    {
        try
        {
            return await _dbContext.Registrations
                .Include(r => r.Category)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving registration {RegistrationId}: {ErrorMessage}",
                id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Update the photo blob name for a registration
    /// </summary>
    public async Task UpdatePhotoBlobAsync(int registrationId, string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be empty", nameof(blobName));

        try
        {
            var registration = await GetRegistrationAsync(registrationId);
            if (registration == null)
            {
                throw new InvalidOperationException($"Registration not found: {registrationId}");
            }

            registration.PhotoBlobName = blobName;
            registration.UpdatedAt = DateTimeProvider.IstNow;

            _dbContext.Registrations.Update(registration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Photo blob updated for registration {RegistrationId}. Blob: {BlobName}",
                registrationId, blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating photo blob for registration {RegistrationId}: {ErrorMessage}",
                registrationId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Update image blob names and URLs for a registration.
    /// Only non-null values overwrite existing ones.
    /// </summary>
    public async Task UpdateImagesAsync(
        int registrationId,
        string? photoBlobName, string? photoUrl,
        string? aadharFrontBlobName, string? aadharFrontUrl,
        string? aadharBackBlobName, string? aadharBackUrl)
    {
        try
        {
            var registration = await GetRegistrationAsync(registrationId);
            if (registration == null)
            {
                throw new InvalidOperationException($"Registration not found: {registrationId}");
            }

            if (!string.IsNullOrWhiteSpace(photoBlobName))
            {
                registration.PhotoBlobName = photoBlobName;
                registration.PhotoUrl = photoUrl;
            }

            if (!string.IsNullOrWhiteSpace(aadharFrontBlobName))
            {
                registration.AadharFrontBlobName = aadharFrontBlobName;
                registration.AadharFrontUrl = aadharFrontUrl;
            }

            if (!string.IsNullOrWhiteSpace(aadharBackBlobName))
            {
                registration.AadharBackBlobName = aadharBackBlobName;
                registration.AadharBackUrl = aadharBackUrl;
            }

            registration.UpdatedAt = DateTimeProvider.IstNow;

            _dbContext.Registrations.Update(registration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Images updated for registration {RegistrationId}.", registrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating images for registration {RegistrationId}: {ErrorMessage}",
                registrationId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Update registration status
    /// </summary>
    public async Task UpdateRegistrationStatusAsync(int registrationId, RegistrationStatus status)
    {
        try
        {
            var registration = await GetRegistrationAsync(registrationId);
            if (registration == null)
            {
                throw new InvalidOperationException($"Registration not found: {registrationId}");
            }

            var oldStatus = registration.Status;
            registration.Status = status;
            registration.UpdatedAt = DateTimeProvider.IstNow;

            // If confirming, set the confirmation timestamp
            if (status == RegistrationStatus.Confirmed && registration.ConfirmedAt == null)
            {
                registration.ConfirmedAt = DateTimeProvider.IstNow;
            }

            _dbContext.Registrations.Update(registration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Registration {RegistrationId} status updated from {OldStatus} to {NewStatus}",
                registrationId, oldStatus, status);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating registration {RegistrationId} status: {ErrorMessage}",
                registrationId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Confirm a registration after successful payment
    /// </summary>
    public async Task ConfirmRegistrationAsync(int registrationId)
    {
        await UpdateRegistrationStatusAsync(registrationId, RegistrationStatus.Confirmed);
    }

    /// <summary>
    /// Get payment by Razorpay Order ID
    /// </summary>
    public async Task<Payment?> GetPaymentByRazorpayOrderIdAsync(string razorpayOrderId)
    {
        try
        {
            return await _dbContext.Payments
                .Include(p => p.Registration)
                .FirstOrDefaultAsync(p => p.RazorpayOrderId == razorpayOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving payment by Razorpay Order ID {OrderId}: {ErrorMessage}",
                razorpayOrderId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Update payment status and Razorpay payment ID after webhook confirmation
    /// </summary>
    public async Task UpdatePaymentAfterWebhookAsync(int paymentId, string razorpayPaymentId, PaymentStatus status)
    {
        try
        {
            var payment = await _dbContext.Payments.FindAsync(paymentId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for PaymentId: {PaymentId}", paymentId);
                throw new InvalidOperationException($"Payment {paymentId} not found");
            }

            payment.RazorpayPaymentId = razorpayPaymentId;
            payment.Status = status;
            payment.UpdatedAt = DateTimeProvider.IstNow;

            _dbContext.Payments.Update(payment);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment updated. PaymentId: {PaymentId}, RazorpayPaymentId: {RazorpayPaymentId}, Status: {Status}",
                paymentId, razorpayPaymentId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating payment after webhook. PaymentId: {PaymentId}, Error: {ErrorMessage}",
                paymentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get all registrations with optional status filtering
    /// </summary>
    public async Task<List<Registration>> GetAllRegistrationsAsync(RegistrationStatus? status = null)
    {
        try
        {
            var query = _dbContext.Registrations
                .Include(r => r.Category)
                .Include(r => r.Payments)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving registrations: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    public async Task<List<Category>> GetCategoriesAsync()
    {
        try
        {
            return await _dbContext.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving categories: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Validate registration input
    /// </summary>
    private static void ValidateRegistrationInput(CreateRegistrationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(dto.Address))
            throw new ArgumentException("Address is required");

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            throw new ArgumentException("Phone number is required");

        if (string.IsNullOrWhiteSpace(dto.Taluka))
            throw new ArgumentException("Taluka is required");

        if (string.IsNullOrWhiteSpace(dto.TShirtSize))
            throw new ArgumentException("T-shirt size is required");

        if (dto.CategoryId <= 0)
            throw new ArgumentException("Valid category is required");

        // Validate lengths
        if (dto.Name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters");

        if (dto.Address.Length > 500)
            throw new ArgumentException("Address cannot exceed 500 characters");

        if (dto.PhoneNumber.Length > 20)
            throw new ArgumentException("Phone number cannot exceed 20 characters");

        if (dto.Taluka.Length > 100)
            throw new ArgumentException("Taluka cannot exceed 100 characters");

        if (dto.TShirtSize.Length > 10)
            throw new ArgumentException("T-shirt size cannot exceed 10 characters");
    }
}
