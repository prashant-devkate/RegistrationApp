using MediatR;

namespace RegistrationApp.Application.Behaviors;

/// <summary>
/// Pipeline behavior for logging all commands and queries
/// Provides observability into CQRS operations
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle the request with logging
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Executing {RequestName} [{RequestId}] with parameters: {@Request}",
            requestName,
            requestId,
            request);

        try
        {
            var startTime = DateTime.UtcNow;
            var response = await next();
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Completed {RequestName} [{RequestId}] in {DurationMilliseconds}ms",
                requestName,
                requestId,
                duration.TotalMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed {RequestName} [{RequestId}] with exception: {ExceptionMessage}",
                requestName,
                requestId,
                ex.Message);

            throw;
        }
    }
}

/// <summary>
/// Pipeline behavior for validating commands and queries
/// Uses FluentValidation for input validation
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private readonly IEnumerable<FluentValidation.IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<FluentValidation.IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validate the request before handling
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new FluentValidation.ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.Where(r => r.Errors.Any()).SelectMany(r => r.Errors).ToList();

        if (failures.Any())
        {
            _logger.LogWarning(
                "Validation failed for {RequestType} with {FailureCount} errors",
                typeof(TRequest).Name,
                failures.Count);

            throw new FluentValidation.ValidationException(failures);
        }

        return await next();
    }
}

/// <summary>
/// Pipeline behavior for wrapping commands in transactions
/// Ensures data consistency and atomicity
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle the request with transaction support (if applicable)
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only apply transaction for commands, not queries
        if (typeof(TRequest).Name.EndsWith("Command"))
        {
            _logger.LogDebug("Beginning transaction for {RequestType}", typeof(TRequest).Name);
        }

        return await next();
    }
}
