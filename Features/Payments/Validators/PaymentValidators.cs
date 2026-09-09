using FluentValidation;
using RegistrationApp.Core.Constants;

namespace RegistrationApp.Features.Payments.Validators;

/// <summary>
/// Validator for PaymentVerificationRequest
/// Ensures payment verification data is present and properly formatted
/// </summary>
public class PaymentVerificationRequestValidator : AbstractValidator<DTOs.PaymentVerificationRequest>
{
    public PaymentVerificationRequestValidator()
    {
        // Razorpay Payment ID validation
        RuleFor(x => x.RazorpayPaymentId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("Payment ID is required");

        RuleFor(x => x.RazorpayPaymentId)
            .MinimumLength(10)
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("Invalid payment ID format");

        // Razorpay Order ID validation
        RuleFor(x => x.RazorpayOrderId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("Order ID is required");

        RuleFor(x => x.RazorpayOrderId)
            .MinimumLength(5)
            .WithErrorCode(ErrorCodes.ValidationError)
            .WithMessage("Invalid order ID format");

        // Razorpay Signature validation
        RuleFor(x => x.RazorpaySignature)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.InvalidPaymentSignature)
            .WithMessage(Messages.ErrorPaymentSignatureVerification);

        RuleFor(x => x.RazorpaySignature)
            .Length(64)
            .WithErrorCode(ErrorCodes.InvalidPaymentSignature)
            .WithMessage("Invalid signature format");
    }
}
