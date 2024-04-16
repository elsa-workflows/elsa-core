using System.Linq.Expressions;
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
    public static async Task BulkUpsertAsync<TDbContext, TEntity>(this TDbContext dbContext, IList<TEntity> entities, Expression<Func<TEntity, string>> keySelector, CancellationToken cancellationToken = default) where TDbContext : DbContext where TEntity : class, new()
    {
        var set = dbContext.Set<TEntity>();
        var compiledKeySelector = keySelector.Compile();
        var containsLambda = entities.Any() ? keySelector.BuildContainsExpression(entities) : default;
        var existingEntitiesQuery = set.AsNoTracking();

        if (containsLambda != null)
            existingEntitiesQuery = existingEntitiesQuery.Where(containsLambda);

        var existingEntities = await existingEntitiesQuery.ToListAsync(cancellationToken);
        var entitiesToUpdate = entities.IntersectBy(existingEntities.Select(compiledKeySelector), compiledKeySelector).ToList();
        var entitiesToInsert = entities.Except(entitiesToUpdate).ToList();

        if (entitiesToUpdate.Any())
            set.UpdateRange(entitiesToUpdate);
        if (entitiesToInsert.Any())
            await set.AddRangeAsync(entitiesToInsert, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a list of entities in bulk.
    /// </summary>
    public static async Task BulkInsertAsync<TDbContext, TEntity>(this TDbContext dbContext, IList<TEntity> entities, CancellationToken cancellationToken = default) where TDbContext : DbContext where TEntity : class, new()
    {
        var set = dbContext.Set<TEntity>();

        if (entities.Any())
            await set.AddRangeAsync(entities, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
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