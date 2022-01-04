using Elsa.Persistence.Models;

namespace Elsa.Runtime.Extensions;

public static class EnumerableExtensions
{
    public static PagedList<T> Paginate<T>(this IQueryable<T> queryable, PagerParameters pagerParameters)
    {
        var (skip, take) = pagerParameters;
        var count = queryable.Count();

        var results = queryable.Skip(skip).Take(take).ToList();
        return new PagedList<T>(results, take, count);
    }
}