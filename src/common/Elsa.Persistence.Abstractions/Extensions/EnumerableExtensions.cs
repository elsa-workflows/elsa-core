using Elsa.Persistence.Common.Models;

namespace Elsa.Persistence.Common.Extensions;

public static class EnumerableExtensions
{
    public static Page<TTarget> Paginate<T, TTarget>(this IEnumerable<T> queryable, Func<T, TTarget> projection, PageArgs? pageArgs = default)
    {
        var items = queryable.ToList();
        var count = items.Count;

        if (pageArgs?.Offset != null) items = items.Skip(pageArgs.Offset.Value).ToList();
        if (pageArgs?.Limit != null) items = items.Take(pageArgs.Limit.Value).ToList();

        var results = items.Select(projection).ToList();
        return Page.Of(results, count);
    }

    public static Page<T> Paginate<T>(this IEnumerable<T> queryable, PageArgs? pageArgs = default)
    {
        var items = queryable.ToList();
        var count = items.Count;

        if (pageArgs?.Offset != null) items = items.Skip(pageArgs.Offset.Value).ToList();
        if (pageArgs?.Limit != null) items = items.Take(pageArgs.Limit.Value).ToList();

        var results = items.ToList();
        return Page.Of(results, count);
    }
}