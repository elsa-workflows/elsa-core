using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// A generic repository class around EF Core for accessing entities.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
[PublicAPI]
public class Store<TDbContext, TEntity> where TDbContext : DbContext where TEntity : class
{
    // ReSharper disable once StaticMemberInGenericType
    // Justification: This is a static member that is used to ensure that only one thread can access the database for TEntity at a time.
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private readonly IDbContextFactory<TDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Store{TDbContext, TEntity}"/> class.
    /// </summary>
    public Store(IDbContextFactory<TDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Creates a new instance of the database context.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The database context.</returns>
    public async Task<TDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    /// <summary>
    /// Adds the specified entity.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await AddAsync(entity, null, cancellationToken);
    }

    /// <summary>
    /// Adds the specified entity.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="onAdding">The callback to invoke before adding the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddAsync(TEntity entity, Func<TDbContext, TEntity, CancellationToken, ValueTask<TEntity>>? onAdding, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        entity = onAdding != null ? await onAdding(dbContext, entity, cancellationToken) : entity;
        var set = dbContext.Set<TEntity>();
        await set.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves the entity.
    /// </summary>
    /// <param name="entity">The entity to save.</param>
    /// <param name="keySelector">The key selector to get the primary key property.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(TEntity entity, Expression<Func<TEntity, string>> keySelector, CancellationToken cancellationToken = default) => await SaveAsync(entity, keySelector, null, cancellationToken);

    /// <summary>
    /// Saves the entity.
    /// </summary>
    /// <param name="entity">The entity to save.</param>
    /// <param name="keySelector">The key selector to get the primary key property.</param>
    /// <param name="onSaving">The callback to invoke before saving the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(TEntity entity, Expression<Func<TEntity, string>> keySelector, Func<TDbContext, TEntity, CancellationToken, ValueTask>? onSaving, CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken); // Asynchronous wait

        try
        {
            await using var dbContext = await CreateDbContextAsync(cancellationToken);

            if (onSaving != null)
                await onSaving(dbContext, entity, cancellationToken);

            var set = dbContext.Set<TEntity>();
            var lambda = keySelector.BuildEqualsExpression(entity);
            var exists = await set.AnyAsync(lambda, cancellationToken);
            set.Entry(entity).State = exists ? EntityState.Modified : EntityState.Added;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// Saves the specified entities.
    /// </summary>
    /// <param name="entities">The entities to save.</param>
    /// <param name="keySelector">The key selector to get the primary key property.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, string>> keySelector, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, keySelector, default, cancellationToken);

    /// <summary>
    /// Saves the specified entities.
    /// </summary>
    /// <param name="entities">The entities to save.</param>
    /// <param name="keySelector">The key selector to get the primary key property.</param>
    /// <param name="onSaving">The callback to invoke before saving the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector,
        Func<TDbContext, TEntity, CancellationToken, ValueTask>? onSaving = default,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var entityList = entities.ToList();

        if (onSaving != null)
        {
            var savingTasks = entityList.Select(entity => onSaving(dbContext, entity, cancellationToken).AsTask()).ToList();
            await Task.WhenAll(savingTasks);
        }

        await dbContext.BulkUpsertAsync(entityList, keySelector, cancellationToken);
    }

    /// <summary>
    /// Finds the entity matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity if found, otherwise <c>null</c>.</returns>
    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) => await FindAsync(predicate, default, cancellationToken);

    /// <summary>
    /// Finds the entity matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to use.</param>
    /// <param name="onLoading">A callback to run after the entity is loaded</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<TDbContext, TEntity?, TEntity?>? onLoading = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var entity = await set.FirstOrDefaultAsync(predicate, cancellationToken);

        if (entity == null)
            return null;

        if (onLoading != default)
            entity = onLoading.Invoke(dbContext, entity);

        return entity;
    }

    /// <summary>
    /// Finds a single entity using a query
    /// </summary>
    /// <param name="query">The query to use</param>
    /// <param name="onLoading">A callback to run after the entity is loaded</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The entity if found, otherwise <c>null</c></returns>
    public async Task<TEntity?> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TDbContext, TEntity?, CancellationToken, ValueTask>? onLoading = default, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(query, onLoading, cancellationToken).FirstOrDefault();
    }

    /// <summary>
    /// Finds a single entity using a query
    /// </summary>
    /// <param name="query">The query to use</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The entity if found, otherwise <c>null</c></returns>
    public async Task<TEntity?> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(query, cancellationToken).FirstOrDefault();
    }

    /// <summary>
    /// Finds a list of entities using a query
    /// </summary>
    public async Task<IEnumerable<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) => await FindManyAsync(predicate, default, cancellationToken);

    /// <summary>
    /// Finds a list of entities using a query
    /// </summary>
    public async Task<IEnumerable<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate, Action<TDbContext, TEntity?>? onLoading = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var entities = await set.Where(predicate).ToListAsync(cancellationToken);

        if (onLoading != null)
            foreach (var entity in entities)
                onLoading(dbContext, entity);

        return entities;
    }

    /// <summary>
    /// Finds a list of entities using a query
    /// </summary>
    public async Task<Page<TEntity>> FindManyAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        OrderDirection orderDirection = OrderDirection.Ascending,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default) =>
        await FindManyAsync(predicate, orderBy, orderDirection, pageArgs, default, cancellationToken);

    /// <summary>
    /// Finds a list of entities using a query
    /// </summary>
    public async Task<Page<TEntity>> FindManyAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        OrderDirection orderDirection = OrderDirection.Ascending,
        PageArgs? pageArgs = default,
        Func<TDbContext, TEntity?, TEntity?>? onLoading = default,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking().Where(predicate);

        set = orderDirection switch
        {
            OrderDirection.Ascending => set.OrderBy(orderBy),
            OrderDirection.Descending => set.OrderByDescending(orderBy),
            _ => set.OrderBy(orderBy)
        };

        var page = await set.PaginateAsync(pageArgs);

        if (onLoading != null)
            page = new Page<TEntity>(page.Items.Select(x => onLoading(dbContext, x)!).ToList(), page.TotalCount);

        return page;
    }

    /// <summary>
    /// Finds a single entity using a query.
    /// </summary>
    /// <returns>True if the entity was found, otherwise false.</returns>
    public async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        set.Attach(entity).State = EntityState.Deleted;
        return await dbContext.SaveChangesAsync(cancellationToken) == 1;
    }

    /// <summary>
    /// Deletes entities using a predicate.
    /// </summary>
    /// <returns>The number of entities deleted.</returns>
    public async Task<long> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        return await set.Where(predicate).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes entities using a query.
    /// </summary>
    /// <returns>The number of entities deleted.</returns>
    public async Task<long> DeleteWhereAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var queryable = query(set.AsQueryable());
        return await queryable.ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query.
    /// </summary>
    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default) => await QueryAsync(query, default, cancellationToken);

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TDbContext, TEntity?, CancellationToken, ValueTask>? onLoading = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var asNoTracking = onLoading == null;
        var set = asNoTracking ? dbContext.Set<TEntity>().AsNoTracking() : dbContext.Set<TEntity>();
        var queryable = query(set.AsQueryable());

        var entities = await queryable.ToListAsync(cancellationToken);

        if (onLoading != null)
        {
            var loadingTasks = entities.Select(entity => onLoading(dbContext, entity, cancellationToken).AsTask()).ToList();
            await Task.WhenAll(loadingTasks);
        }

        return entities;
    }

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Expression<Func<TEntity, TResult>> selector, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var queryable = query(set.AsQueryable());

        queryable = query(queryable);
        return await queryable.Select(selector).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Counts the number of entities matching a query.
    /// </summary>
    public async Task<long> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var queryable = query(set.AsQueryable());

        queryable = query(queryable);
        return await queryable.LongCountAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Checks if any entities exist.
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        return await set.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Counts the number of entities matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        return await set.CountAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Counts the distinct number of entities matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="propertySelector">The property selector to distinct by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<long> CountAsync<TProperty>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProperty>> propertySelector, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        return await set
            .Where(predicate)
            .Select(propertySelector)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}