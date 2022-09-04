using System.Linq.Expressions;
using Elsa.Common.Models;

namespace Elsa.Common.Extensions;

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

    public static Page<T> Paginate<T>(this IQueryable<T> queryable, PageArgs? pageArgs = default)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);

        var count = queryable.Count();
        var results = queryable.ToList();
        return Page.Of(results, count);
    }
}