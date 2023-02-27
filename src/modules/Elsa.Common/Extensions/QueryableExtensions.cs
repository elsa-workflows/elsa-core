using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class QueryableExtensions
{
    public static Page<TTarget> Paginate<T, TTarget>(this IQueryable<T> queryable, Expression<Func<T, TTarget>> projection, PageArgs? pageArgs = default)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);

        var count = queryable.Count();
        var results = queryable.Select(projection).ToList();
        return Page.Of(results, count);
    }

    public static Page<T> ToPage<T>(this IQueryable<T> queryable, PageArgs? pageArgs = default)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
    
        var count = queryable.Count();
        var results = queryable.ToList();
        return Page.Of(results, count);
    }
    
    public static IQueryable<T> Paginate<T>(this IQueryable<T> queryable, PageArgs pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);

        return queryable;
    }

    public static IQueryable<T> OrderBy<T, TOrderBy>(this IQueryable<T> queryable, OrderDefinition<T, TOrderBy> order) =>
        order.Direction == OrderDirection.Ascending 
            ? queryable.OrderBy(order.KeySelector) 
            : queryable.OrderByDescending(order.KeySelector);
}