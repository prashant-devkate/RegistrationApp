using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrationApp.Core.Constants;
using RegistrationApp.Models;
using RegistrationApp.Services;

namespace RegistrationApp.Pages;

/// <summary>
/// Page model for payment processing
/// Displays payment details and handles payment initiation via Razorpay
/// </summary>
public class PaymentModel : PageModel
{
    private readonly IRegistrationService _registrationService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentModel> _logger;

    public PaymentModel(IRegistrationService registrationService, IPaymentService paymentService, ILogger<PaymentModel> logger)
    {
        _registrationService = registrationService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public Registration? Registration { get; set; }
    public Category? Category { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayKeyId { get; set; }

    /// <summary>
    /// GET: Load registration and category details, create Razorpay order
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            _logger.LogInformation("Payment page OnGetAsync called with id: {RegistrationId}", id);

            // Get registration by ID
            Registration = await _registrationService.GetRegistrationAsync(id);

            if (Registration == null)
            {
                _logger.LogWarning("Registration not found. RegistrationId: {RegistrationId}", id);
                return RedirectToPage("/Index");
            }

            _logger.LogInformation("Registration found: {RegistrationId}, CategoryId: {CategoryId}", Registration.Id, Registration.CategoryId);

            // Get all categories and find the matching one
            var categories = await _registrationService.GetCategoriesAsync();
            _logger.LogInformation("Categories retrieved. Count: {Count}", categories.Count);

            Category = categories.FirstOrDefault(c => c.Id == Registration.CategoryId);

            if (Category == null)
            {
                _logger.LogWarning("Category not found for registration. RegistrationId: {RegistrationId}, CategoryId: {CategoryId}", id, Registration.CategoryId);
                return RedirectToPage("/Index");
            }

            _logger.LogInformation("Category found: {CategoryName}, Fee: {Fee}", Category.Name, Category.RegistrationFee);

            // Create Razorpay order
            try
            {
                _logger.LogInformation("Creating Razorpay order for RegistrationId: {RegistrationId}", id);
                var orderResponse = await _paymentService.CreateOrderAsync(id);
                RazorpayOrderId = orderResponse.Id;
                _logger.LogInformation("Razorpay order created successfully. OrderId: {OrderId}", RazorpayOrderId);
            }
            catch (Exception orderEx)
            {
                _logger.LogError(orderEx, "Failed to create Razorpay order for RegistrationId: {RegistrationId}. Error: {Error}", id, orderEx.Message);
                ErrorMessage = $"Failed to create payment order: {orderEx.Message}";
                throw;
            }

            // Get Razorpay Key ID from configuration for client-side checkout
            RazorpayKeyId = HttpContext?.RequestServices.GetRequiredService<IConfiguration>()?["Razorpay:KeyId"];
            _logger.LogInformation("RazorpayKeyId loaded: {KeyId}", RazorpayKeyId);

            _logger.LogInformation(
                "Payment page loaded. RegistrationId: {RegistrationId}, Phone: {Phone}, Amount: {Amount}, RazorpayOrderId: {OrderId}",
                Registration.Id,
                Registration.PhoneNumber,
                Category.RegistrationFee,
                RazorpayOrderId
            );

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payment page for RegistrationId: {RegistrationId}. Exception: {Exception}", id, ex.ToString());
            ErrorMessage = Messages.ErrorGenericSystemError;
            return RedirectToPage("/Index");
        }
    }
}

