using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Elsa.Common.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to <see cref="IQueryable{T}"/>.
/// </summary>
[PublicAPI]
public static class QueryableExtensions
{
    /// <summary>
    /// Inserts or updates a list of entities in bulk.
    /// </summary>
    public static async Task BulkUpsertAsync<TDbContext, TEntity>(this TDbContext dbContext, IList<TEntity> entities, Expression<Func<TEntity, string>> keySelector, CancellationToken cancellationToken = default) where TDbContext : DbContext where TEntity : class
    {
        await dbContext.BulkInsertOrUpdateAsync(entities, new BulkConfig
        {
            EnableShadowProperties = true
        }, cancellationToken: cancellationToken);
    }

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