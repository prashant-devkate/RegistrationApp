using MediatR;

namespace RegistrationApp.Application.Interfaces;

/// <summary>
/// Base interface for all commands
/// Commands represent intent to change state
/// </summary>
public interface ICommand : IRequest
{
}

/// <summary>
/// Base interface for commands that return a result
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Base interface for all queries
/// Queries represent intent to retrieve data without changing state
/// </summary>
public interface IQuery : IRequest
{
}

/// <summary>
/// Base interface for queries that return a result
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Base interface for command handlers
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}

/// <summary>
/// Base interface for command handlers that return a result
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResult">The result type</typeparam>
public interface ICommandHandler<TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
}

/// <summary>
/// Base interface for query handlers
/// </summary>
/// <typeparam name="TQuery">The query type</typeparam>
public interface IQueryHandler<TQuery> : IRequestHandler<TQuery>
    where TQuery : IQuery
{
}

/// <summary>
/// Base interface for query handlers that return a result
/// </summary>
/// <typeparam name="TQuery">The query type</typeparam>
/// <typeparam name="TResult">The result type</typeparam>
public interface IQueryHandler<TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
}
