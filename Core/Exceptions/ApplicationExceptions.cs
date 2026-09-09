using RegistrationApp.Core.Constants;

namespace RegistrationApp.Core.Exceptions;

/// <summary>
/// Base exception for application-specific exceptions
/// Includes error code for consistent error handling
/// </summary>
public class ApplicationException : Exception
{
    /// <summary>
    /// Error code for this exception
    /// Used for error mapping and i18n
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// User-friendly error message
    /// </summary>
    public string UserMessage { get; }

    public ApplicationException(string errorCode, string message, string userMessage) 
        : base(message)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        UserMessage = userMessage ?? throw new ArgumentNullException(nameof(userMessage));
    }

    public ApplicationException(string errorCode, string message, string userMessage, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        UserMessage = userMessage ?? throw new ArgumentNullException(nameof(userMessage));
    }
}

/// <summary>
/// Business logic exception
/// Thrown when a business rule is violated
/// </summary>
public class BusinessException : ApplicationException
{
    public BusinessException(string errorCode, string message, string userMessage)
        : base(errorCode, message, userMessage)
    {
    }

    public BusinessException(string errorCode, string message, string userMessage, Exception innerException)
        : base(errorCode, message, userMessage, innerException)
    {
    }

    /// <summary>
    /// Create a business exception for not found scenarios
    /// </summary>
    public static BusinessException NotFound(string entityName, int id)
        => new(
            ErrorCodes.NotFound,
            $"{entityName} with ID {id} not found",
            Messages.ErrorGenericSystemError);

    /// <summary>
    /// Create a business exception for duplicate scenarios
    /// </summary>
    public static BusinessException Duplicate(string entityName, string fieldName, string fieldValue)
        => new(
            ErrorCodes.DuplicateEmail,
            $"{entityName} with {fieldName} '{fieldValue}' already exists",
            Messages.ErrorDuplicateEmail);
}

/// <summary>
/// Validation exception
/// Thrown when input validation fails
/// </summary>
public class ValidationException : ApplicationException
{
    /// <summary>
    /// List of validation errors
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string errorCode, string message, string userMessage, IReadOnlyDictionary<string, string[]> errors)
        : base(errorCode, message, userMessage)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Create a validation exception from FluentValidation results
    /// </summary>
    public static ValidationException FromFluentValidation(FluentValidation.ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new ValidationException(
            ErrorCodes.ValidationError,
            "One or more validation errors occurred",
            Messages.ErrorGenericSystemError,
            errors);
    }
}

/// <summary>
/// Authentication exception
/// Thrown when authentication fails
/// </summary>
public class AuthenticationException : ApplicationException
{
    public AuthenticationException(string message, string userMessage)
        : base(ErrorCodes.AuthenticationRequired, message, userMessage)
    {
    }
}

/// <summary>
/// Authorization exception
/// Thrown when user lacks required permissions
/// </summary>
public class AuthorizationException : ApplicationException
{
    public AuthorizationException(string message, string userMessage)
        : base(ErrorCodes.AccessDenied, message, userMessage)
    {
    }
}

/// <summary>
/// Payment exception
/// Thrown when payment operations fail
/// </summary>
public class PaymentException : ApplicationException
{
    public PaymentException(string errorCode, string message, string userMessage)
        : base(errorCode, message, userMessage)
    {
    }

    public PaymentException(string errorCode, string message, string userMessage, Exception innerException)
        : base(errorCode, message, userMessage, innerException)
    {
    }
}

/// <summary>
/// External service exception
/// Thrown when external services (Razorpay, Azure) fail
/// </summary>
public class ExternalServiceException : ApplicationException
{
    /// <summary>
    /// External service name
    /// </summary>
    public string ServiceName { get; }

    public ExternalServiceException(string serviceName, string message)
        : base(
            ErrorCodes.ExternalServiceError,
            $"{serviceName} error: {message}",
            Messages.ErrorExternalServiceError)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base(
            ErrorCodes.ExternalServiceError,
            $"{serviceName} error: {message}",
            Messages.ErrorExternalServiceError,
            innerException)
    {
        ServiceName = serviceName;
    }
}
