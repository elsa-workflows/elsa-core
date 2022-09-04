using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Elsa.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions;

public static class QueryableExtensions
{
    public static async Task<int> BatchDeleteWithWorkAroundAsync<T>(this IQueryable<T> queryable, DbContext elsaContext, CancellationToken cancellationToken = default) where T : class
    {
        if (elsaContext.Database.IsPostgres() || elsaContext.Database.IsMySql() || elsaContext.Database.IsOracle())
        {
            // Need this workaround https://github.com/borisdj/EFCore.BulkExtensions/issues/553 is solved.
            // Oracle also https://github.com/borisdj/EFCore.BulkExtensions/issues/375
            var records = await queryable.ToListAsync(cancellationToken);

            foreach (var @record in records) 
                elsaContext.Remove(@record);

            return records.Count;
        }

        return await queryable.BatchDeleteAsync(cancellationToken);
    }
    
    public static async Task<Page<TTarget>> PaginateAsync<T, TTarget>(this IQueryable<T> queryable, Expression<Func<T, TTarget>> projection, PageArgs? pageArgs = default)
    {
        var count = await queryable.CountAsync();
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        var results = await queryable.Select(projection).ToListAsync();
        return Page.Of(results, count);
    }
    
    public static async Task<Page<T>> PaginateAsync<T>(this IQueryable<T> queryable, PageArgs? pageArgs = default)
    {
        var count = await queryable.CountAsync();
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        var results = await queryable.ToListAsync();
        return Page.Of(results, count);
    }
}