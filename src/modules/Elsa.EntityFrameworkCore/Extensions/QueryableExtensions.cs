using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using EFCore.BulkExtensions;
using Elsa.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Deletes the matching results in bulk.
    /// </summary>
    public static async Task<int> BulkDeleteAsync<T>(this IQueryable<T> queryable, DbContext elsaContext, CancellationToken cancellationToken = default) where T : class
    {
#if NET7_0_OR_GREATER
            return await queryable.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
#else
        if (!elsaContext.Database.IsPostgres() && !elsaContext.Database.IsMySql() && !elsaContext.Database.IsOracle()) 
            return await queryable.BatchDeleteAsync(cancellationToken);
        
        // Need this workaround https://github.com/borisdj/EFCore.BulkExtensions/issues/553 is solved.
        // Oracle also https://github.com/borisdj/EFCore.BulkExtensions/issues/375
        var records = await queryable.ToListAsync(cancellationToken);

        foreach (var record in records)
            elsaContext.Remove(record);

        return records.Count;

#endif
    }

    /// <summary>
    /// Inserts or updates a list of entities in bulk.
    /// </summary>
    public static async Task BulkUpsertAsync<TDbContext, TEntity>(this TDbContext dbContext, IList<TEntity> entities, CancellationToken cancellationToken = default) where TDbContext : DbContext where TEntity : class => 
        await dbContext.BulkInsertOrUpdateAsync(entities, config => { config.EnableShadowProperties = true; }, cancellationToken: cancellationToken);

    /// <summary>
    /// Returns a paged result from the specified query.
    /// </summary>
    public static async Task<Page<TTarget>> PaginateAsync<T, TTarget>(this IQueryable<T> queryable, Expression<Func<T, TTarget>> projection, PageArgs? pageArgs = default)
    {
        var count = await queryable.CountAsync();
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        var results = await queryable.Select(projection).ToListAsync();
        return Page.Of(results, count);
    }

    /// <summary>
    /// Returns a paged result from the specified query.
    /// </summary>
    public static async Task<Page<T>> PaginateAsync<T>(this IQueryable<T> queryable, PageArgs? pageArgs = default)
    {
        var count = await queryable.CountAsync();
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        var results = await queryable.ToListAsync();
        return Page.Of(results, count);
    }
}