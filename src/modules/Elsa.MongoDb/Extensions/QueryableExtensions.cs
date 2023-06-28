using Elsa.Common.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Returns a paged result from the specified query.
    /// </summary>
    public static async Task<Page<T>> PaginateAsync<T>(this IMongoQueryable<T> queryable, PageArgs? pageArgs = default)
    {
        var count = await queryable.CountAsync();
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        var results = await queryable.ToListAsync();
        return Page.Of(results, count);
    }
}