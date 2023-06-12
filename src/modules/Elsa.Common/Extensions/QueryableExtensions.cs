using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> objects.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Paginates the queryable.
    /// </summary>
    /// <param name="queryable">The queryable to paginate.</param>
    /// <param name="projection">The projection to apply to the queryable.</param>
    /// <param name="pageArgs">The pagination arguments.</param>
    /// <typeparam name="T">The type of the queryable.</typeparam>
    /// <typeparam name="TTarget">The type of the projection.</typeparam>
    /// <returns>A page of the projected queryable.</returns>
    public static Page<TTarget> Paginate<T, TTarget>(this IQueryable<T> queryable, Expression<Func<T, TTarget>> projection, PageArgs? pageArgs = default)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);

        var count = queryable.Count();
        var results = queryable.Select(projection).ToList();
        return Page.Of(results, count);
    }

    /// <summary>
    /// Paginates the queryable.
    /// </summary>
    /// <param name="queryable">The queryable to paginate.</param>
    /// <param name="pageArgs">The pagination arguments.</param>
    /// <typeparam name="T">The type of the queryable.</typeparam>
    /// <returns>A page of the queryable.</returns>
    public static Page<T> ToPage<T>(this IQueryable<T> queryable, PageArgs? pageArgs = default)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
    
        var count = queryable.Count();
        var results = queryable.ToList();
        return Page.Of(results, count);
    }
    
    /// <summary>
    /// Paginates the queryable.
    /// </summary>
    /// <param name="queryable">The queryable to paginate.</param>
    /// <param name="pageArgs">The pagination arguments.</param>
    /// <typeparam name="T">The type of the queryable.</typeparam>
    /// <returns>The paginated queryable.</returns>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> queryable, PageArgs pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);

        return queryable;
    }

    /// <summary>
    /// Orders the queryable by the specified order.
    /// </summary>
    /// <param name="queryable">The queryable to order.</param>
    /// <param name="order">The order to apply to the queryable.</param>
    /// <typeparam name="T">The type of the queryable.</typeparam>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>The ordered queryable.</returns>
    public static IQueryable<T> OrderBy<T, TOrderBy>(this IQueryable<T> queryable, OrderDefinition<T, TOrderBy> order) =>
        order.Direction == OrderDirection.Ascending 
            ? queryable.OrderBy(order.KeySelector) 
            : queryable.OrderByDescending(order.KeySelector);
}