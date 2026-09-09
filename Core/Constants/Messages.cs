namespace RegistrationApp.Core.Constants;

/// <summary>
/// Centralized user-facing messages for consistent communication
/// Messages are localized and user-friendly
/// </summary>
public static class Messages
{
    // ===== Success Messages =====
    /// <summary>
    /// Registration created successfully
    /// </summary>
    public const string RegistrationCreatedSuccessfully = "Your registration has been created successfully. Please proceed to payment to complete your registration.";

    /// <summary>
    /// Payment verified successfully
    /// </summary>
    public const string PaymentVerifiedSuccessfully = "Payment verified successfully. Your registration is now confirmed!";

    /// <summary>
    /// Payment processed successfully
    /// </summary>
    public const string PaymentProcessedSuccessfully = "Your payment has been processed successfully. Thank you for registering!";

    /// <summary>
    /// Photo uploaded successfully
    /// </summary>
    public const string PhotoUploadedSuccessfully = "Photo uploaded successfully.";

    /// <summary>
    /// Registration confirmed
    /// </summary>
    public const string RegistrationConfirmed = "Your registration is confirmed. Thank you for completing the registration process.";

    // ===== Validation Error Messages =====
    /// <summary>
    /// Name is required
    /// </summary>
    public const string ErrorNameRequired = "Full name is required.";

    /// <summary>
    /// Name exceeds maximum length
    /// </summary>
    public const string ErrorNameTooLong = "Full name cannot exceed 200 characters.";

    /// <summary>
    /// Address is required
    /// </summary>
    public const string ErrorAddressRequired = "Address is required.";

    /// <summary>
    /// Address exceeds maximum length
    /// </summary>
    public const string ErrorAddressTooLong = "Address cannot exceed 500 characters.";

    /// <summary>
    /// Email is required
    /// </summary>
    public const string ErrorEmailRequired = "Email address is required.";

    /// <summary>
    /// Email format is invalid
    /// </summary>
    public const string ErrorInvalidEmailFormat = "Please enter a valid email address.";

    /// <summary>
    /// Email exceeds maximum length
    /// </summary>
    public const string ErrorEmailTooLong = "Email cannot exceed 100 characters.";

    /// <summary>
    /// Phone number is required
    /// </summary>
    public const string ErrorPhoneNumberRequired = "Phone number is required.";

    /// <summary>
    /// Phone number format is invalid
    /// </summary>
    public const string ErrorInvalidPhoneNumber = "Please enter a valid phone number.";

    /// <summary>
    /// Phone number exceeds maximum length
    /// </summary>
    public const string ErrorPhoneNumberTooLong = "Phone number cannot exceed 20 characters.";

    /// <summary>
    /// Category is required
    /// </summary>
    public const string ErrorCategoryRequired = "Please select a category.";

    /// <summary>
    /// Category is invalid
    /// </summary>
    public const string ErrorInvalidCategory = "The selected category is invalid. Please select a valid category.";

    /// <summary>
    /// Photo is required
    /// </summary>
    public const string ErrorPhotoRequired = "Please upload a photo.";

    /// <summary>
    /// Aadhar front image is required
    /// </summary>
    public const string ErrorAadharFrontRequired = "Please upload the Aadhar front image.";

    /// <summary>
    /// Aadhar back image is required
    /// </summary>
    public const string ErrorAadharBackRequired = "Please upload the Aadhar back image.";

    /// <summary>
    /// Photo file size exceeds maximum
    /// </summary>
    public const string ErrorPhotoSizeTooLarge = "Photo size cannot exceed 10 MB.";

    /// <summary>
    /// Photo file type is invalid
    /// </summary>
    public const string ErrorPhotoInvalidFileType = "Only image files (.jpg, .png, .gif, .webp) are allowed.";

    // ===== Business Logic Error Messages =====
    /// <summary>
    /// Registration not found
    /// </summary>
    public const string ErrorRegistrationNotFound = "Registration not found. Please check your registration ID and try again.";

    /// <summary>
    /// Category not found
    /// </summary>
    public const string ErrorCategoryNotFound = "Category not found. Please contact support.";

    /// <summary>
    /// Duplicate email - registration already exists
    /// </summary>
    public const string ErrorDuplicateEmail = "An active registration with this email address already exists. Please use a different email or log in to your existing registration.";

    /// <summary>
    /// Payment signature verification failed
    /// </summary>
    public const string ErrorPaymentSignatureVerification = "Payment signature verification failed. Please contact support if this issue persists.";

    /// <summary>
    /// Payment amount mismatch
    /// </summary>
    public const string ErrorPaymentAmountMismatch = "Payment amount does not match the registration fee. Please contact support.";

    /// <summary>
    /// Payment currency mismatch
    /// </summary>
    public const string ErrorPaymentCurrencyMismatch = "Payment currency mismatch. Please contact support.";

    /// <summary>
    /// Payment processing failed
    /// </summary>
    public const string ErrorPaymentProcessingFailed = "Payment processing failed. Please try again or contact support.";

    /// <summary>
    /// Payment already processed
    /// </summary>
    public const string ErrorPaymentAlreadyProcessed = "This payment has already been processed. If you need to make another payment, please create a new registration.";

    /// <summary>
    /// Razorpay order creation failed
    /// </summary>
    public const string ErrorRazorpayOrderCreationFailed = "Unable to create a payment order. Please try again or contact support.";

    /// <summary>
    /// Photo upload failed
    /// </summary>
    public const string ErrorPhotoUploadFailed = "Photo upload failed. Please try again or contact support.";

    /// <summary>
    /// Photo download failed
    /// </summary>
    public const string ErrorPhotoDownloadFailed = "Unable to retrieve your photo. Please contact support.";

    /// <summary>
    /// Validation failed
    /// </summary>
    public const string ErrorValidationFailed = "Form validation failed. Please check all required fields and try again.";

    /// <summary>
    /// Database connection error
    /// </summary>
    public const string ErrorDatabaseConnection = "Unable to connect to the database. Please try again later.";

    /// <summary>
    /// Database write error
    /// </summary>
    public const string ErrorDatabaseWrite = "Failed to save your registration. Please try again or contact support.";

    // ===== System Error Messages =====
    /// <summary>
    /// Generic error message for unexpected errors
    /// </summary>
    public const string ErrorGenericSystemError = "An unexpected error occurred. Please try again later or contact support.";

    /// <summary>
    /// Database error
    /// </summary>
    public const string ErrorDatabaseError = "A database error occurred. Please try again later.";

    /// <summary>
    /// Service temporarily unavailable
    /// </summary>
    public const string ErrorServiceUnavailable = "The service is temporarily unavailable. Please try again later.";

    /// <summary>
    /// External service error
    /// </summary>
    public const string ErrorExternalServiceError = "An external service error occurred. Please try again later.";

    // ===== Informational Messages =====
    /// <summary>
    /// Payment pending
    /// </summary>
    public const string InfoPaymentPending = "Your payment is pending. Please complete the payment process.";

    /// <summary>
    /// Payment failed
    /// </summary>
    public const string InfoPaymentFailed = "Your payment failed. Please try again or use a different payment method.";

    /// <summary>
    /// Please accept terms and conditions
    /// </summary>
    public const string WarningAcceptTerms = "Please accept the terms and conditions to proceed.";

    // ===== Confirmation Messages =====
    /// <summary>
    /// Confirm registration deletion
    /// </summary>
    public const string ConfirmDeleteRegistration = "Are you sure you want to delete this registration?";

    /// <summary>
    /// Confirm payment retry
    /// </summary>
    public const string ConfirmRetryPayment = "You have a pending payment. Would you like to retry?";
}
