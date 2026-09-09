using FluentValidation;
using RegistrationApp.Core.Constants;

namespace RegistrationApp.Features.Registrations.Validators;

/// <summary>
/// Validator for CreateRegistrationRequest
/// Ensures all input data is valid before processing
/// </summary>
public class CreateRegistrationRequestValidator : AbstractValidator<DTOs.CreateRegistrationRequest>
{
    public CreateRegistrationRequestValidator()
    {
        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.NameRequired)
            .WithMessage(Messages.ErrorNameRequired);

        RuleFor(x => x.Name)
            .MaximumLength(ApplicationConstants.MaxNameLength)
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage($"Name cannot exceed {ApplicationConstants.MaxNameLength} characters");

        // Address validation
        RuleFor(x => x.Address)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.AddressRequired)
            .WithMessage(Messages.ErrorAddressRequired);

        RuleFor(x => x.Address)
            .MaximumLength(ApplicationConstants.MaxAddressLength)
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage($"Address cannot exceed {ApplicationConstants.MaxAddressLength} characters");

        // Taluka validation
        RuleFor(x => x.Taluka)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("Taluka is required");

        RuleFor(x => x.Taluka)
            .Must(t => RegistrationOptions.IsValidTaluka(t))
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("Invalid Taluka selected");

        // T-shirt size validation
        RuleFor(x => x.TShirtSize)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("T-shirt size is required");

        RuleFor(x => x.TShirtSize)
            .Must(s => RegistrationOptions.IsValidTShirtSize(s))
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("Invalid T-shirt size selected");

        // Phone number validation
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.PhoneNumberRequired)
            .WithMessage(Messages.ErrorPhoneNumberRequired);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(ApplicationConstants.MaxPhoneNumberLength)
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage($"Phone number cannot exceed {ApplicationConstants.MaxPhoneNumberLength} characters");

        // Category validation
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithErrorCode(ErrorCodes.CategoryRequired)
            .WithMessage(Messages.ErrorCategoryRequired);
    }
}

/// <summary>
/// Validator for UpdateRegistrationStatusRequest
/// </summary>
public class UpdateRegistrationStatusRequestValidator : AbstractValidator<DTOs.UpdateRegistrationStatusRequest>
{
    public UpdateRegistrationStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("Status is required");

        RuleFor(x => x.Status)
            .Must(status => IsValidStatus(status))
            .WithErrorCode(ErrorCodes.InvalidRegistrationStatus)
            .WithMessage("Invalid registration status");
    }

    /// <summary>
    /// Check if the provided status is valid
    /// </summary>
    private static bool IsValidStatus(string status)
    {
        return status switch
        {
            "PaymentPending" or "PaymentFailed" or "Confirmed" or "Cancelled" => true,
            _ => false
        };
    }
}
