using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Inserts or updates a list of entities in bulk.
    /// </summary>
    public static async Task BulkUpsertAsync<TDbContext, TEntity>(this TDbContext dbContext, IList<TEntity> entities, Expression<Func<TEntity, object>>? uniqueFieldExpression = default, CancellationToken cancellationToken = default) where TDbContext : DbContext where TEntity : class
    {
        uniqueFieldExpression = ResolveUniqueFieldExpression(uniqueFieldExpression);
        var uniqueFieldDelegate = uniqueFieldExpression.Compile();
        var propertyInfo = uniqueFieldExpression.GetProperty()!;

        var set = dbContext.Set<TEntity>();
        var lambda = uniqueFieldDelegate.BuildContainsExpression(entities, propertyInfo);

        var existingEntities = await set.AsNoTracking().Where(lambda).ToListAsync(cancellationToken);
        var entitiesToUpdate = entities.Where(e => existingEntities.Any(ex => uniqueFieldDelegate.Invoke(ex).ToString() == uniqueFieldDelegate.Invoke(e).ToString())).ToList();
        var entitiesToInsert = entities.Except(entitiesToUpdate).ToList();

        if (entitiesToUpdate.Any())
            set.UpdateRange(entitiesToUpdate);
        if (entitiesToInsert.Any())
            await set.AddRangeAsync(entitiesToInsert, cancellationToken);

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

    private static Expression<Func<TEntity, object>> ResolveUniqueFieldExpression<TEntity>(Expression<Func<TEntity, object>>? uniqueFieldExpression) where TEntity : class
    {
        if (uniqueFieldExpression != null) return uniqueFieldExpression;
        try
        {
            uniqueFieldExpression = e => ((Entity)(object)e).Id;
        }
        catch (Exception)
        {
            throw new Exception(
                "Unique field expression must be passed via BulkUpsertAsync if default object to upsert is not of type Entity.");
        }

        return uniqueFieldExpression;
    }
}