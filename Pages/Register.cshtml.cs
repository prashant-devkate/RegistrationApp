using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RegistrationApp.Core.Constants;
using RegistrationApp.Models;
using RegistrationApp.Services;

namespace RegistrationApp.Pages;

/// <summary>
/// Page model for user registration form
/// Handles form display and submission without blob storage or payment processing
/// </summary>
public class RegisterModel : PageModel
{
    private readonly IRegistrationService _registrationService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        IRegistrationService registrationService,
        IBlobStorageService blobStorageService,
        ILogger<RegisterModel> logger)
    {
        _registrationService = registrationService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    [BindProperty]
    public RegistrationFormInput? Input { get; set; }

    public List<CategoryDto> Categories { get; set; } = new();
    public string[] TalukaOptions { get; set; } = RegistrationOptions.TalukaOptions;
    public string[] TShirtSizeOptions { get; set; } = RegistrationOptions.TShirtSizes;
    public bool ShowSuccess { get; set; }
    public int RegistrationId { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// GET: Load categories and display form
    /// </summary>
    public async Task OnGetAsync()
    {
        try
        {
            var categories = await _registrationService.GetCategoriesAsync();
            Categories = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                RegistrationFee = (int)c.RegistrationFee,
                Currency = c.Currency
            }).ToList();

            _logger.LogInformation("Categories loaded for registration form. Count: {CategoryCount}", Categories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories");
            ErrorMessage = Messages.ErrorGenericSystemError;
        }
    }

    /// <summary>
    /// POST: Create registration from form input
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Clear previous errors
            ValidationErrors.Clear();

            // Validate input
            if (!ModelState.IsValid)
            {
                ErrorMessage = Messages.ErrorValidationFailed;
                _logger.LogWarning("Registration form validation failed");
                await OnGetAsync();
                return Page();
            }

            if (Input == null)
            {
                ErrorMessage = Messages.ErrorGenericSystemError;
                await OnGetAsync();
                return Page();
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(Input.FullName))
            {
                ValidationErrors["FullName"] = Messages.ErrorNameRequired;
            }

            if (string.IsNullOrWhiteSpace(Input.PhoneNumber))
            {
                ValidationErrors["PhoneNumber"] = Messages.ErrorPhoneNumberRequired;
            }

            if (string.IsNullOrWhiteSpace(Input.Address))
            {
                ValidationErrors["Address"] = Messages.ErrorAddressRequired;
            }

            if (string.IsNullOrWhiteSpace(Input.Taluka))
            {
                ValidationErrors["Taluka"] = "Taluka is required";
            }

            if (string.IsNullOrWhiteSpace(Input.TShirtSize))
            {
                ValidationErrors["TShirtSize"] = "T-shirt size is required";
            }

            if (Input.CategoryId <= 0)
            {
                ValidationErrors["CategoryId"] = Messages.ErrorCategoryRequired;
            }

            if (Input.Photo is not { Length: > 0 })
            {
                ValidationErrors["Photo"] = Messages.ErrorPhotoRequired;
            }

            if (Input.AadharFront is not { Length: > 0 })
            {
                ValidationErrors["AadharFront"] = Messages.ErrorAadharFrontRequired;
            }

            if (Input.AadharBack is not { Length: > 0 })
            {
                ValidationErrors["AadharBack"] = Messages.ErrorAadharBackRequired;
            }

            // Return to form if validation errors exist
            if (ValidationErrors.Count > 0)
            {
                ErrorMessage = Messages.ErrorValidationFailed;
                await OnGetAsync();
                return Page();
            }

            // Create registration via service
            var createDto = new CreateRegistrationDto
            {
                Name = Input.FullName.Trim(),
                PhoneNumber = Input.PhoneNumber.Trim(),
                Address = Input.Address.Trim(),
                Taluka = Input.Taluka.Trim(),
                TShirtSize = Input.TShirtSize.Trim(),
                CategoryId = Input.CategoryId,
                PhotoBlobName = null,  // Will be set during blob upload later
                AadharFrontBlobName = null,
                AadharBackBlobName = null
            };

            var registration = await _registrationService.CreateRegistrationAsync(createDto);

            _logger.LogInformation(
                "Registration created successfully. RegistrationId: {RegistrationId}, Phone: {PhoneNumber}, Category: {CategoryId}",
                registration.Id,
                registration.PhoneNumber,
                registration.CategoryId
            );

            // Upload images to Azure Blob Storage and persist their names + URLs
            await UploadImagesAsync(registration.Id);

            // Redirect to payment page with registration ID
            return RedirectToPage("/Payment", new { id = registration.Id });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Connection"))
        {
            _logger.LogError(ex, "Database connection error during registration");
            ErrorMessage = Messages.ErrorDatabaseConnection;
            await OnGetAsync();
            return Page();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during registration creation");
            ErrorMessage = Messages.ErrorDatabaseWrite;
            await OnGetAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration");
            ErrorMessage = Messages.ErrorGenericSystemError;
            await OnGetAsync();
            return Page();
        }
    }

    /// <summary>
    /// Uploads any provided images to Azure Blob Storage and stores their blob names + URLs.
    /// Upload failures are logged but do not fail the registration.
    /// </summary>
    private async Task UploadImagesAsync(int registrationId)
    {
        try
        {
            string? photoBlob = null, photoUrl = null;
            string? frontBlob = null, frontUrl = null;
            string? backBlob = null, backUrl = null;

            if (Input?.Photo is { Length: > 0 } photo)
            {
                (photoBlob, photoUrl) = await UploadOneAsync(registrationId, photo, "photo");
            }

            if (Input?.AadharFront is { Length: > 0 } front)
            {
                (frontBlob, frontUrl) = await UploadOneAsync(registrationId, front, "aadhar-front");
            }

            if (Input?.AadharBack is { Length: > 0 } back)
            {
                (backBlob, backUrl) = await UploadOneAsync(registrationId, back, "aadhar-back");
            }

            if (photoBlob != null || frontBlob != null || backBlob != null)
            {
                await _registrationService.UpdateImagesAsync(
                    registrationId,
                    photoBlob, photoUrl,
                    frontBlob, frontUrl,
                    backBlob, backUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images for registration {RegistrationId}", registrationId);
        }
    }

    private async Task<(string? blobName, string? url)> UploadOneAsync(int registrationId, IFormFile file, string imageType)
    {
        await using var stream = file.OpenReadStream();
        var blobName = await _blobStorageService.UploadPhotoAsync(registrationId, stream, file.FileName, imageType);
        var url = _blobStorageService.GetPhotoUrl(blobName);
        return (blobName, url);
    }

    }

    /// <summary>
    /// Form input model for registration
    /// </summary>
    public class RegistrationFormInput
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Taluka { get; set; }
    public string? TShirtSize { get; set; }
    public int CategoryId { get; set; }
    public IFormFile? Photo { get; set; }
    public IFormFile? AadharFront { get; set; }
    public IFormFile? AadharBack { get; set; }
}

/// <summary>
/// Category DTO for display in dropdown
/// </summary>
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RegistrationFee { get; set; }
    public string Currency { get; set; } = "INR";
}
