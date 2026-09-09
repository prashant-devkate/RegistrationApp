namespace RegistrationApp.Core.Constants;

/// <summary>
/// Centralized error codes for consistent error handling across the application
/// </summary>
public static class ErrorCodes
{
    // ===== Validation Errors =====
    /// <summary>
    /// General validation error
    /// </summary>
    public const string ValidationError = "VALIDATION_ERROR";

    /// <summary>
    /// Name is required
    /// </summary>
    public const string NameRequired = "NAME_REQUIRED";

    /// <summary>
    /// Address is required
    /// </summary>
    public const string AddressRequired = "ADDRESS_REQUIRED";

    /// <summary>
    /// Email is required
    /// </summary>
    public const string EmailRequired = "EMAIL_REQUIRED";

    /// <summary>
    /// Email format is invalid
    /// </summary>
    public const string InvalidEmailFormat = "INVALID_EMAIL_FORMAT";

    /// <summary>
    /// Phone number is required
    /// </summary>
    public const string PhoneNumberRequired = "PHONE_REQUIRED";

    /// <summary>
    /// Category is required
    /// </summary>
    public const string CategoryRequired = "CATEGORY_REQUIRED";

    /// <summary>
    /// Photo is required
    /// </summary>
    public const string PhotoRequired = "PHOTO_REQUIRED";

    /// <summary>
    /// Photo file size exceeds maximum
    /// </summary>
    public const string PhotoSizeTooLarge = "PHOTO_SIZE_TOO_LARGE";

    /// <summary>
    /// Photo file type is not allowed
    /// </summary>
    public const string PhotoInvalidFileType = "PHOTO_INVALID_FILE_TYPE";

    // ===== Registration Errors =====
    /// <summary>
    /// Registration not found
    /// </summary>
    public const string RegistrationNotFound = "REGISTRATION_NOT_FOUND";

    /// <summary>
    /// Duplicate email - registration already exists
    /// </summary>
    public const string DuplicateEmail = "DUPLICATE_EMAIL";

    /// <summary>
    /// Invalid registration status
    /// </summary>
    public const string InvalidRegistrationStatus = "INVALID_REGISTRATION_STATUS";

    /// <summary>
    /// Registration cannot be modified in current state
    /// </summary>
    public const string RegistrationStateViolation = "REGISTRATION_STATE_VIOLATION";

    // ===== Category Errors =====
    /// <summary>
    /// Category not found
    /// </summary>
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";

    /// <summary>
    /// Invalid category selection
    /// </summary>
    public const string InvalidCategory = "INVALID_CATEGORY";

    // ===== Payment Errors =====
    /// <summary>
    /// Payment not found
    /// </summary>
    public const string PaymentNotFound = "PAYMENT_NOT_FOUND";

    /// <summary>
    /// Payment signature verification failed
    /// </summary>
    public const string InvalidPaymentSignature = "INVALID_PAYMENT_SIGNATURE";

    /// <summary>
    /// Payment amount mismatch
    /// </summary>
    public const string PaymentAmountMismatch = "PAYMENT_AMOUNT_MISMATCH";

    /// <summary>
    /// Payment currency mismatch
    /// </summary>
    public const string PaymentCurrencyMismatch = "PAYMENT_CURRENCY_MISMATCH";

    /// <summary>
    /// Payment processing failed
    /// </summary>
    public const string PaymentProcessingFailed = "PAYMENT_PROCESSING_FAILED";

    /// <summary>
    /// Payment already processed
    /// </summary>
    public const string PaymentAlreadyProcessed = "PAYMENT_ALREADY_PROCESSED";

    /// <summary>
    /// Razorpay order creation failed
    /// </summary>
    public const string RazorpayOrderCreationFailed = "RAZORPAY_ORDER_CREATION_FAILED";

    /// <summary>
    /// Invalid payment status
    /// </summary>
    public const string InvalidPaymentStatus = "INVALID_PAYMENT_STATUS";

    // ===== Webhook Errors =====
    /// <summary>
    /// Webhook signature verification failed
    /// </summary>
    public const string InvalidWebhookSignature = "INVALID_WEBHOOK_SIGNATURE";

    /// <summary>
    /// Webhook payload is malformed
    /// </summary>
    public const string MalformedWebhookPayload = "MALFORMED_WEBHOOK_PAYLOAD";

    /// <summary>
    /// Unhandled webhook event type
    /// </summary>
    public const string UnhandledWebhookEvent = "UNHANDLED_WEBHOOK_EVENT";

    // ===== Azure Storage Errors =====
    /// <summary>
    /// Photo upload to Azure Blob Storage failed
    /// </summary>
    public const string PhotoUploadFailed = "PHOTO_UPLOAD_FAILED";

    /// <summary>
    /// Photo download from Azure Blob Storage failed
    /// </summary>
    public const string PhotoDownloadFailed = "PHOTO_DOWNLOAD_FAILED";

    /// <summary>
    /// Photo not found in Azure Blob Storage
    /// </summary>
    public const string PhotoNotFound = "PHOTO_NOT_FOUND";

    /// <summary>
    /// Photo deletion failed
    /// </summary>
    public const string PhotoDeletionFailed = "PHOTO_DELETION_FAILED";

    // ===== Database Errors =====
    /// <summary>
    /// Database operation failed
    /// </summary>
    public const string DatabaseError = "DATABASE_ERROR";

    /// <summary>
    /// Concurrency error - record was modified by another user
    /// </summary>
    public const string ConcurrencyError = "CONCURRENCY_ERROR";

    /// <summary>
    /// Transaction failed
    /// </summary>
    public const string TransactionFailed = "TRANSACTION_FAILED";

    // ===== Authorization Errors =====
    /// <summary>
    /// Access denied
    /// </summary>
    public const string AccessDenied = "ACCESS_DENIED";

    /// <summary>
    /// Authentication required
    /// </summary>
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";

    // ===== System Errors =====
    /// <summary>
    /// General system error
    /// </summary>
    public const string SystemError = "SYSTEM_ERROR";

    /// <summary>
    /// Resource not found (404)
    /// </summary>
    public const string NotFound = "NOT_FOUND";

    /// <summary>
    /// Internal server error
    /// </summary>
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";

    /// <summary>
    /// Service unavailable
    /// </summary>
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";

    /// <summary>
    /// External service error
    /// </summary>
    public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
}
