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
/// A decorator for <see cref="IBookmarkStore"/> that caches bookmark records.
/// </summary>
public class CachingBookmarkStore(
    IBookmarkStore decoratedStore, 
    IMemoryCache cache, 
    IHasher hasher, 
    IChangeTokenSignaler changeTokenSignaler,
    IOptions<CachingOptions> cachingOptions) : IBookmarkStore
{
    private static readonly string CacheInvalidationTokenKey = typeof(CachingBookmarkStore).FullName!;

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
        await decoratedStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken)
    {
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
        await decoratedStore.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
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
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        changeTokenSignaler.TriggerToken(CacheInvalidationTokenKey);
        return await decoratedStore.DeleteAsync(filter, cancellationToken);
    }
}