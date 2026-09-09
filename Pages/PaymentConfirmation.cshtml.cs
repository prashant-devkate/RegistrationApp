using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrationApp.Core.Constants;
using RegistrationApp.Models;
using RegistrationApp.Services;

namespace RegistrationApp.Pages;

public class PaymentConfirmationModel : PageModel
{
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<PaymentConfirmationModel> _logger;

    public PaymentConfirmationModel(IRegistrationService registrationService, ILogger<PaymentConfirmationModel> logger)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Display properties
    public int RegistrationId { get; set; }
    public string RegistrationName { get; set; } = string.Empty;
    public string RegistrationPhone { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            _logger.LogInformation("Payment confirmation page accessed for RegistrationId: {RegistrationId}", id);

            // Get registration with details
            var registration = await _registrationService.GetRegistrationAsync(id);

            if (registration == null)
            {
                _logger.LogWarning("Registration not found for RegistrationId: {RegistrationId}", id);
                return RedirectToPage("/Index");
            }

            RegistrationId = registration.Id;
            RegistrationName = registration.Name ?? "N/A";
            RegistrationPhone = registration.PhoneNumber ?? "N/A";

            // Get category details
            var categories = await _registrationService.GetCategoriesAsync();
            var category = categories.FirstOrDefault(c => c.Id == registration.CategoryId);
            CategoryName = category?.Name ?? "N/A";
            AmountPaid = ApplicationConstants.RegistrationFee;

            // Check registration status
            if (registration.Status == RegistrationStatus.Confirmed)
            {
                IsSuccess = true;
                _logger.LogInformation(
                    "Payment confirmation successful. RegistrationId: {RegistrationId}, Phone: {Phone}",
                    id,
                    RegistrationPhone
                );
            }
            else
            {
                IsSuccess = false;
                ErrorMessage = $"Registration status is {registration.Status}. Please try again or contact support.";
                _logger.LogWarning(
                    "Registration not confirmed. RegistrationId: {RegistrationId}, Status: {Status}",
                    id,
                    registration.Status
                );
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payment confirmation page for RegistrationId: {RegistrationId}", id);
            ErrorMessage = "An error occurred while loading the confirmation page.";
            return Page();
        }
    }
}
