using Elsa.Common.Contracts;
using Elsa.Common.Options;
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
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        var cacheEntry = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            var invalidationRequestToken = changeTokenSignaler.GetToken(CacheInvalidationTokenKey);
            entry.AddExpirationToken(invalidationRequestToken);
            entry.SetAbsoluteExpiration(cachingOptions.Value.CacheDuration);
            return (await decoratedStore.FindManyAsync(filter, cancellationToken)).ToList();
        });
        
        return cacheEntry!;
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
}