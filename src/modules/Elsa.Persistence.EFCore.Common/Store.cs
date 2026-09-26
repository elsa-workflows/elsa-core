using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Extensions;
using Elsa.Tenants.Options;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// A generic repository class around EF Core for accessing entities.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
[PublicAPI]
public class Store<TDbContext, TEntity>(IDbContextFactory<TDbContext> dbContextFactory, IServiceProvider serviceProvider) where TDbContext : DbContext where TEntity : class, new()
{
    private const int WriteMaxRetryCount = 3;
    private static readonly TimeSpan WriteBaseDelay = TimeSpan.FromMilliseconds(50);

    // ReSharper disable once StaticMemberInGenericType
    // Justification: This is a static member that is used to ensure that only one thread can access the database for TEntity at a time.
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    /// <summary>
    /// Creates a new instance of the database context.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The database context.</returns>
    public async Task<TDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
    public async Task AddAsync(TEntity entity, Func<TDbContext, TEntity, CancellationToken, ValueTask>? onAdding, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);

        if (onAdding != null)
            await onAdding(dbContext, entity, cancellationToken);

        var set = dbContext.Set<TEntity>();
        await set.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds the specified entities.
    /// </summary>
    /// <param name="entities">The entities to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddManyAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        await AddManyAsync(entities, null, cancellationToken);
    }

    /// <summary>
    /// Adds the specified entities.
    /// </summary>
    /// <param name="entities">The entities to save.</param>
    /// <param name="onSaving">The callback to invoke before saving the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddManyAsync(
        IEnumerable<TEntity> entities,
        Func<TDbContext, TEntity, CancellationToken, ValueTask>? onSaving = null,
        CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);

        try
        {
            var entityList = entities.ToList();

            if (entityList.Count == 0)
                return;

            await ExecuteWriteWithRetryAsync(async (dbContext, ct) =>
            {
                if (onSaving != null)
                {
                    var savingTasks = entityList.Select(entity => onSaving(dbContext, entity, ct).AsTask()).ToList();
                    await Task.WhenAll(savingTasks);
                }

                await dbContext.BulkInsertAsync(entityList, ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleDbExceptionAsync(ex, cancellationToken);
            throw;
        }
        finally
        {
            Semaphore.Release();
        }
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
        catch (Exception ex)
        {
            await HandleDbExceptionAsync(ex, cancellationToken);
            throw;
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
    public async Task SaveManyAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, string>> keySelector, CancellationToken cancellationToken = default) => await SaveManyAsync(entities, keySelector, null, cancellationToken);

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
        Func<TDbContext, TEntity, CancellationToken, ValueTask>? onSaving = null,
        CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);

        try
        {
            var entityList = entities.ToList();

            if (entityList.Count == 0)
                return;

            var tenantId = serviceProvider.GetRequiredService<ITenantAccessor>().TenantId;
            var tenancyEnabled = serviceProvider.GetService<IOptions<TenantsOptions>>()?.Value.IsEnabled == true;

            await ExecuteWriteWithRetryAsync(async (dbContext, ct) =>
            {
                if (onSaving != null)
                {
                    var savingTasks = entityList.Select(entity => onSaving(dbContext, entity, ct).AsTask()).ToList();
                    await Task.WhenAll(savingTasks);
                }

                // When doing a custom SQL query (Bulk Upsert), none of the installed query filters will be applied. Hence, we are assigning the current tenant ID explicitly.
                foreach (var entity in entityList)
                {
                    if (entity is Entity entityWithTenant)
                    {
                        // Don't touch tenant-agnostic entities (marked with "*")
                        if (entityWithTenant.TenantId == Tenant.AgnosticTenantId)
                            continue;

                        // Apply current tenant ID to entities without one
                        if (entityWithTenant.TenantId == null)
                            entityWithTenant.TenantId = tenantId;
                    }
                }

                if (tenancyEnabled)
                    await EnsureTenantOwnershipAsync(dbContext, entityList, keySelector, tenantId, ct);

                await dbContext.BulkUpsertAsync(entityList, keySelector, ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleDbExceptionAsync(ex, cancellationToken);
            throw;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// #8490: an existing row may be replaced only when the stamped incoming TenantId is the
    /// writer's own tenant or "*", and Normalize(existing) == Normalize(incoming).
    /// <c>null</c> and "" both count as the default tenant. Forged-TenantId inserts of new keys
    /// are not refused here; the import endpoint clears TenantId so the store stamps the writer.
    /// The lookup and bulk upsert are separate statements; a concurrent insert of a known key
    /// between them can still overwrite. Closing that race is option (ii).
    /// </summary>
    private async Task EnsureTenantOwnershipAsync(
        TDbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector,
        string writerTenantId,
        CancellationToken cancellationToken)
    {
        if (!typeof(Entity).IsAssignableFrom(typeof(TEntity)))
            return;

        var getKey = keySelector.Compile();
        var keyName = keySelector.GetProperty()!.Name;
        var existingByKey = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var keys in entities.Select(getKey).Distinct(StringComparer.Ordinal).Chunk(BulkUpsertExtensions.DefaultBatchSize))
        {
            var keyList = keys.ToList();
            var existingRows = await dbContext.Set<TEntity>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(entity => keyList.Contains(EF.Property<string>(entity, keyName)))
                .Select(entity => new ExistingKeyTenant(
                    EF.Property<string>(entity, keyName),
                    EF.Property<string?>(entity, nameof(Entity.TenantId))))
                .ToListAsync(cancellationToken);

            foreach (var existing in existingRows)
                existingByKey[existing.Key] = existing.TenantId;
        }

        foreach (var entity in entities)
        {
            var key = getKey(entity);
            if (!existingByKey.TryGetValue(key, out var existingTenantId))
                continue;

            var incomingTenantId = ((Entity)(object)entity).TenantId;
            if (!MayReplaceExistingRow(existingTenantId, incomingTenantId, writerTenantId))
                throw new InvalidOperationException($"Cannot replace {typeof(TEntity).Name} '{key}': tenant ownership mismatch. Shared rows need TenantId '*'.");
        }
    }

    /// <summary>
    /// Incoming must already be the writer's tenant or "*"; existing must match that same
    /// normalized value. A tenant literally named "default" is not the default tenant ("").
    /// </summary>
    private static bool MayReplaceExistingRow(string? existingTenantId, string? incomingTenantId, string writerTenantId)
    {
        var existing = existingTenantId.NormalizeTenantId();
        var incoming = incomingTenantId.NormalizeTenantId();

        if (incoming != writerTenantId && incoming != Tenant.AgnosticTenantId)
            return false;

        return existing == incoming;
    }

    private sealed record ExistingKeyTenant(string Key, string? TenantId);

    private async Task HandleDbExceptionAsync(Exception exception, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService<IDbExceptionHandler>();

        if (handler == null)
            return;

        var context = new DbUpdateExceptionContext(exception, cancellationToken);
        await handler.HandleAsync(context);
    }

    /// <summary>
    /// Executes a database operation and passes failures through the configured database
    /// exception handler.
    /// </summary>
    /// <typeparam name="TResult">The operation result type.</typeparam>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="shouldHandle">A predicate that excludes exceptions which are not database failures.</param>
    /// <returns>The result returned by <paramref name="operation"/>.</returns>
    internal async Task<TResult> ExecuteWithDbExceptionHandlingAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default,
        Func<Exception, bool>? shouldHandle = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (shouldHandle is null || shouldHandle(exception))
        {
            await HandleDbExceptionAsync(exception, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Executes a whole database write operation with a fresh context after a provider
    /// transient failure. The caller owns the transaction boundary so a retry never
    /// resumes a partially completed transaction.
    /// </summary>
    internal async Task ExecuteWriteWithRetryAsync(
        Func<TDbContext, CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0;; attempt++)
        {
            var providerName = string.Empty;

            try
            {
                await using var dbContext = await CreateDbContextAsync(cancellationToken);
                providerName = dbContext.Database.ProviderName ?? string.Empty;
                await operation(dbContext, cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                if (ShouldRetryWrite(providerName, ex, attempt, cancellationToken))
                {
                    await Task.Delay(GetWriteRetryDelay(attempt), cancellationToken);
                    continue;
                }

                throw;
            }
        }
    }

    private static bool ShouldRetryWrite(string providerName, Exception exception, int attempt, CancellationToken cancellationToken)
    {
        return attempt < WriteMaxRetryCount
               && !cancellationToken.IsCancellationRequested
               && exception is not OperationCanceledException
               && DbExceptionClassifier.IsTransient(providerName, exception);
    }

    private static TimeSpan GetWriteRetryDelay(int attempt) => TimeSpan.FromMilliseconds(WriteBaseDelay.TotalMilliseconds * (attempt + 1));

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return UpdateAsync(entity, null, cancellationToken);
    }

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="onSaving">The callback to invoke before saving the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task UpdateAsync(TEntity entity, Func<TDbContext, TEntity, CancellationToken, ValueTask>? onSaving, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);

        if (onSaving != null)
            await onSaving(dbContext, entity, cancellationToken);

        var set = dbContext.Set<TEntity>();
        set.Entry(entity).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates specific properties of an entity in the database.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="properties">An array of expressions indicating the properties to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdatePartialAsync(TEntity entity, Expression<Func<TEntity, object>>[] properties, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        dbContext.Attach(entity);

        foreach (var property in properties)
            dbContext.Entry(entity).Property(property).IsModified = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Finds the entity matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity if found, otherwise <c>null</c>.</returns>
    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) => await FindAsync(predicate, null, cancellationToken);

    /// <summary>
    /// Finds the entity matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to use.</param>
    /// <param name="onLoading">A callback to run after the entity is loaded</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<TDbContext, TEntity?, TEntity?>? onLoading = null, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var entity = await set.FirstOrDefaultAsync(predicate, cancellationToken);

        if (entity == null)
            return null;

        if (onLoading != null)
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
    public async Task<TEntity?> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TDbContext, TEntity?, CancellationToken, ValueTask>? onLoading = null, CancellationToken cancellationToken = default)
    {
        return await FindAsync(query, onLoading, false, cancellationToken);
    }

    /// <summary>
    /// Finds a single entity using a query
    /// </summary>
    /// <param name="query">The query to use</param>
    /// <param name="onLoading">A callback to run after the entity is loaded</param>
    /// <param name="tenantAgnostic">Define is the request should be tenant agnostic or not</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The entity if found, otherwise <c>null</c></returns>
    public async Task<TEntity?> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TDbContext, TEntity?, CancellationToken, ValueTask>? onLoading = null, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(query, onLoading, tenantAgnostic, cancellationToken).FirstOrDefault();
    }

    /// <summary>
    /// Finds a single entity using a query
    /// </summary>
    /// <param name="query">The query to use</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The entity if found, otherwise <c>null</c></returns>
    public async Task<TEntity?> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        return await FindAsync(query, false, cancellationToken);
    }

    /// <summary>
    /// Finds a single entity using a query
    /// </summary>
    /// <param name="query">The query to use</param>
    /// <param name="tenantAgnostic">Define is the request should be tenant agnostic or not</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The entity if found, otherwise <c>null</c></returns>
    public async Task<TEntity?> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(query, tenantAgnostic, cancellationToken).FirstOrDefault();
    }

    /// <summary>
    /// Finds a list of entities using a query
    /// </summary>
    public async Task<IEnumerable<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) => await FindManyAsync(predicate, null, cancellationToken);

    /// <summary>
    /// Finds a list of entities using a query
    /// </summary>
    public async Task<IEnumerable<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate, Action<TDbContext, TEntity?>? onLoading = null, CancellationToken cancellationToken = default)
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
        PageArgs? pageArgs = null,
        CancellationToken cancellationToken = default) =>
        await FindManyAsync(predicate, orderBy, orderDirection, pageArgs, null, cancellationToken);

    /// <summary>
    /// Returns a list of entities using a query
    /// </summary>
    public async Task<Page<TEntity>> FindManyAsync<TKey>(
        Expression<Func<TEntity, bool>>? predicate,
        Expression<Func<TEntity, TKey>>? orderBy,
        OrderDirection orderDirection = OrderDirection.Ascending,
        PageArgs? pageArgs = null,
        Func<TDbContext, TEntity?, TEntity?>? onLoading = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();

        if (predicate != null)
            set = set.Where(predicate);

        if (orderBy != null)
            set = orderDirection switch
            {
                OrderDirection.Ascending => set.OrderBy(orderBy),
                OrderDirection.Descending => set.OrderByDescending(orderBy),
                _ => set.OrderBy(orderBy)
            };

        var page = await set.PaginateAsync(pageArgs);

        if (onLoading != null)
            page = page with
            {
                Items = page.Items.Select(x => onLoading(dbContext, x)!).ToList()
            };

        return page;
    }

    public Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return ListAsync(null, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> ListAsync(Action<TDbContext, TEntity?>? onLoading = null, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var entities = await set.ToListAsync(cancellationToken);

        if (onLoading != null)
            foreach (var entity in entities)
                onLoading(dbContext, entity);

        return entities;
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
    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(query, null, false, cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query.
    /// </summary>
    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(query, null, tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TDbContext, TEntity?, CancellationToken, ValueTask>? onLoading = null, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(query, onLoading, false, cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TDbContext, TEntity?, CancellationToken, ValueTask>? onLoading = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var asNoTracking = onLoading == null;
        var set = asNoTracking ? dbContext.Set<TEntity>().AsNoTracking() : dbContext.Set<TEntity>();
        var queryable = query(set.AsQueryable());

        if (ignoreQueryFilters)
            queryable = queryable.IgnoreQueryFilters();

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
        return await QueryAsync(query, selector, false, cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Expression<Func<TEntity, TResult>> selector, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var queryable = query(set.AsQueryable());

        if (ignoreQueryFilters)
            queryable = queryable.IgnoreQueryFilters();

        queryable = query(queryable);
        return await queryable.Select(selector).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Counts the number of entities matching a query.
    /// </summary>
    public async Task<long> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default)
    {
        return await CountAsync(query, false, cancellationToken);
    }

    /// <summary>
    /// Counts the number of entities matching a query.
    /// </summary>
    public async Task<long> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<TEntity>().AsNoTracking();
        var queryable = query(set.AsQueryable());

        if (ignoreQueryFilters)
            queryable = queryable.IgnoreQueryFilters();

        queryable = query(queryable);
        return await queryable.LongCountAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Checks if any entities exist.
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await AnyAsync(predicate, false, cancellationToken);
    }

    /// <summary>
    /// Checks if any entities exist.
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
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
        return await CountAsync(predicate, false, cancellationToken);
    }

    /// <summary>
    /// Counts the number of entities matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="ignoreQueryFilters">Whether to ignore query filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var queryable = dbContext.Set<TEntity>().AsNoTracking();

        if (ignoreQueryFilters)
            queryable = queryable.IgnoreQueryFilters();

        return await queryable.CountAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Counts the distinct number of entities matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="propertySelector">The property selector to distinct by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<long> CountAsync<TProperty>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProperty>> propertySelector, CancellationToken cancellationToken = default)
    {
        return await CountAsync(predicate, propertySelector, false, cancellationToken);
    }

    /// <summary>
    /// Counts the distinct number of entities matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="propertySelector">The property selector to distinct by.</param>
    /// <param name="ignoreQueryFilters">Whether to ignore query filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<long> CountAsync<TProperty>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProperty>> propertySelector, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await CreateDbContextAsync(cancellationToken);
        var queryable = dbContext.Set<TEntity>().AsNoTracking();

        if (ignoreQueryFilters)
            queryable = queryable.IgnoreQueryFilters();

        return await queryable
            .Where(predicate)
            .Select(propertySelector)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}
