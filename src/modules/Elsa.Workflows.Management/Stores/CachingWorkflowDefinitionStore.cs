using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Common.Options;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.Stores;

/// <summary>
/// A decorator for <see cref="IWorkflowDefinitionStore"/> that caches workflow definitions.
/// </summary>
public class CachingWorkflowDefinitionStore(
    IWorkflowDefinitionStore decoratedStore, 
    IMemoryCache cache, 
    IHasher hasher, 
    IChangeTokenSignaler changeTokenSignaler,
    IOptions<CachingOptions> cachingOptions) : IWorkflowDefinitionStore
{
    private static readonly string CacheInvalidationTokenKey = typeof(CachingWorkflowDefinitionStore).FullName!;

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return await GetOrCreateAsync(() => decoratedStore.FindAsync(filter, cancellationToken), cacheKey);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order);
        return await GetOrCreateAsync(() => decoratedStore.FindAsync(filter, order, cancellationToken), cacheKey);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, pageArgs);
        return (await GetOrCreateAsync(() => decoratedStore.FindManyAsync(filter, pageArgs, cancellationToken), cacheKey))!;
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order, pageArgs);
        return (await GetOrCreateAsync(() => decoratedStore.FindManyAsync(filter, order, pageArgs, cancellationToken), cacheKey))!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return (await GetOrCreateAsync(() => decoratedStore.FindManyAsync(filter, cancellationToken), cacheKey))!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order);
        return (await GetOrCreateAsync(() => decoratedStore.FindManyAsync(filter, order, cancellationToken), cacheKey))!;
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, pageArgs);
        return (await GetOrCreateAsync(() => decoratedStore.FindSummariesAsync(filter, pageArgs, cancellationToken), cacheKey))!;
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order, pageArgs);
        return (await GetOrCreateAsync(() => decoratedStore.FindSummariesAsync(filter, order, pageArgs, cancellationToken), cacheKey))!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return (await GetOrCreateAsync(() => decoratedStore.FindSummariesAsync(filter, cancellationToken), cacheKey))!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter, order);
        return (await GetOrCreateAsync(() => decoratedStore.FindSummariesAsync(filter, order, cancellationToken), cacheKey))!;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var cacheKey = hasher.Hash(filter);
        return await GetOrCreateAsync(() => decoratedStore.FindLastVersionAsync(filter, cancellationToken), cacheKey);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        await decoratedStore.SaveAsync(definition, cancellationToken);
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        await decoratedStore.SaveManyAsync(definitions, cancellationToken);
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await decoratedStore.DeleteAsync(filter, cancellationToken);
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(nameof(AnyAsync), filter);
        return await GetOrCreateAsync(() => decoratedStore.AnyAsync(filter, cancellationToken), cacheKey);
    }

    /// <inheritdoc />
    public async Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(nameof(CountDistinctAsync));
        return await GetOrCreateAsync(() => decoratedStore.CountDistinctAsync(cancellationToken), cacheKey);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(nameof(GetIsNameUnique), name, definitionId);
        return await GetOrCreateAsync(() => decoratedStore.GetIsNameUnique(name, definitionId, cancellationToken), cacheKey);
    }
    
    private async Task<T?> GetOrCreateAsync<T>(Func<Task<T>> factory, string key)
    {
        var internalKey = $"{typeof(T).Name}:{key}";
        return await cache.GetOrCreateAsync(internalKey, async entry =>
        {
            var invalidationRequestToken = changeTokenSignaler.GetToken(CacheInvalidationTokenKey);
            entry.AddExpirationToken(invalidationRequestToken);
            entry.SetAbsoluteExpiration(cachingOptions.Value.CacheDuration);
            return await factory();
        });
    }
}