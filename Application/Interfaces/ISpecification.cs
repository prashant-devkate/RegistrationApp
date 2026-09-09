using System.Linq.Expressions;

namespace RegistrationApp.Application.Interfaces;

/// <summary>
/// Specification pattern abstraction for building reusable, testable queries
/// Encapsulates queries into objects, making them more maintainable and composable
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class Specification<T> where T : class
{
    /// <summary>
    /// The criteria/filter for the query
    /// </summary>
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    /// <summary>
    /// List of includes to eager load related entities
    /// </summary>
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    /// <summary>
    /// String-based includes for more complex navigation properties
    /// </summary>
    public List<string> IncludeStrings { get; } = new();

    /// <summary>
    /// Ordering function
    /// </summary>
    public Expression<Func<T, object>>? OrderBy { get; protected set; }

    /// <summary>
    /// Descending ordering function
    /// </summary>
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    /// <summary>
    /// Pagination: skip count
    /// </summary>
    public int? Take { get; protected set; }

    /// <summary>
    /// Pagination: take count
    /// </summary>
    public int? Skip { get; protected set; }

    /// <summary>
    /// Is pagination enabled?
    /// </summary>
    public bool IsPagingEnabled { get; protected set; }

    /// <summary>
    /// Add a criteria to filter entities
    /// </summary>
    protected virtual void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = Criteria == null ? criteria : CombineExpressions(Criteria, criteria);
    }

    /// <summary>
    /// Add an include for eager loading
    /// </summary>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Add a string-based include
    /// </summary>
    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Set ordering (ascending)
    /// </summary>
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Set ordering (descending)
    /// </summary>
    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Apply pagination
    /// </summary>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Combine two expressions with AND
    /// </summary>
    private static Expression<Func<T, bool>> CombineExpressions(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftInvoked = Expression.Invoke(left, parameter);
        var rightInvoked = Expression.Invoke(right, parameter);
        var combined = Expression.AndAlso(leftInvoked, rightInvoked);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}

/// <summary>
/// Specification with custom result type (for projections)
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
/// <typeparam name="TResult">The result type after projection</typeparam>
public abstract class Specification<T, TResult> : Specification<T>
    where T : class
{
    /// <summary>
    /// Projection function to transform entity to result type
    /// </summary>
    public Expression<Func<T, TResult>>? Select { get; protected set; }

    /// <summary>
    /// Set the projection
    /// </summary>
    protected virtual void ApplySelect(Expression<Func<T, TResult>> selectExpression)
    {
        Select = selectExpression;
    }
}
