using Elsa.Caching.Contracts;
using Elsa.Caching.Options;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// A decorator for <see cref="ITriggerStore"/> that caches trigger records.
/// </summary>
public class CachingTriggerStore(
    ITriggerStore decoratedStore,
    IMemoryCache cache,
    IHasher hasher,
    IChangeTokenSignaler changeTokenSignaler,
    IOptions<CachingOptions> cachingOptions) : ITriggerStore
{
    private static readonly string CacheInvalidationTokenKey = typeof(CachingTriggerStore).FullName!;

    /// <inheritdoc />
    public ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
        return decoratedStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
        return decoratedStore.SaveManyAsync(records, cancellationToken);
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
    public ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
        return decoratedStore.ReplaceAsync(removed, added, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
        return decoratedStore.DeleteManyAsync(filter, cancellationToken);
    }

    private async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
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