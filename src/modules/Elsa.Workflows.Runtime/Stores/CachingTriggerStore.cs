using Elsa.Caching;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// A decorator for <see cref="ITriggerStore"/> that caches trigger records.
/// </summary>
public class CachingTriggerStore(ITriggerStore decoratedStore, ICacheManager cacheManager, IHasher hasher, ITenantAccessor tenantAccessor) : ITriggerStore
{
    /// <summary>
    /// The token key to evict all cached triggers.
    /// </summary>
    public static readonly string CacheInvalidationTokenKey = typeof(CachingTriggerStore).FullName!;

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        await decoratedStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        await decoratedStore.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return (await GetOrCreateAsync(cacheKey, async () => await decoratedStore.FindAsync(filter, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return (await GetOrCreateAsync(cacheKey, async () => await decoratedStore.FindManyAsync(filter, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        await decoratedStore.ReplaceAsync(removed, added, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        return await decoratedStore.DeleteManyAsync(filter, cancellationToken);
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
            entry.SetAbsoluteExpiration(cacheManager.CachingOptions.Value.CacheDuration);
            return await factory();
        });
    }
}