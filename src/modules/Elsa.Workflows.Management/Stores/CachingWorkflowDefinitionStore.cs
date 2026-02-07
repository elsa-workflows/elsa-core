using Elsa.Caching;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Workflows.Management.Stores;

/// <summary>
/// A decorator for <see cref="IWorkflowDefinitionStore"/> that caches workflow definitions.
/// </summary>
public class CachingWorkflowDefinitionStore(IWorkflowDefinitionStore decoratedStore, ICacheManager cacheManager, IHasher hasher, ITenantAccessor tenantAccessor) : IWorkflowDefinitionStore
{
    private static readonly string CacheInvalidationTokenKey = typeof(CachingWorkflowDefinitionStore).FullName!;

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return await GetOrCreateAsync(cacheKey, () => decoratedStore.FindAsync(filter, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order);
        return await GetOrCreateAsync(cacheKey, () => decoratedStore.FindAsync(filter, order, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, pageArgs);
        return (await GetOrCreateAsync(cacheKey, () => decoratedStore.FindManyAsync(filter, pageArgs, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order, pageArgs);
        return (await GetOrCreateAsync(cacheKey, () => decoratedStore.FindManyAsync(filter, order, pageArgs, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return (await GetOrCreateAsync(cacheKey, async () => (await decoratedStore.FindManyAsync(filter, cancellationToken)).ToList()))!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order);
        return (await GetOrCreateAsync(cacheKey, async () => (await decoratedStore.FindManyAsync(filter, order, cancellationToken)).ToList()))!;
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, pageArgs);
        return (await GetOrCreateAsync(cacheKey, () => decoratedStore.FindSummariesAsync(filter, pageArgs, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order, pageArgs);
        return (await GetOrCreateAsync(cacheKey, () => decoratedStore.FindSummariesAsync(filter, order, pageArgs, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return (await GetOrCreateAsync(cacheKey, () => decoratedStore.FindSummariesAsync(filter, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order);
        return (await GetOrCreateAsync(cacheKey, () => decoratedStore.FindSummariesAsync(filter, order, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var cacheKey = hasher.Hash(filter);
        return await GetOrCreateAsync(cacheKey, () => decoratedStore.FindLastVersionAsync(filter, cancellationToken));
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        await decoratedStore.SaveAsync(definition, cancellationToken);
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        await decoratedStore.SaveManyAsync(definitions, cancellationToken);
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await decoratedStore.DeleteAsync(filter, cancellationToken);
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(nameof(AnyAsync), filter);
        return await GetOrCreateAsync(cacheKey, () => decoratedStore.AnyAsync(filter, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(nameof(CountDistinctAsync));
        return await GetOrCreateAsync(cacheKey, () => decoratedStore.CountDistinctAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(nameof(GetIsNameUnique), name, definitionId);
        return await GetOrCreateAsync(cacheKey, () => decoratedStore.GetIsNameUnique(name, definitionId, cancellationToken));
    }

    private async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        var tenantIdPrefix = !string.IsNullOrEmpty(tenantId) ? $"{tenantId}:" : string.Empty;
        var internalKey = $"{tenantIdPrefix}{typeof(T).Name}:{key}";
        return await cacheManager.GetOrCreateAsync(internalKey, async entry =>
        {
            var invalidationRequestToken = cacheManager.GetToken(CacheInvalidationTokenKey);
            entry.AddExpirationToken(invalidationRequestToken);
            entry.SetSlidingExpiration(cacheManager.CachingOptions.Value.CacheDuration);
            return await factory();
        });
    }
}