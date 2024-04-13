using Elsa.Caching;
using Elsa.Caching.Options;
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
    ICacheManager memoryCache,
    IHasher hasher,
    IChangeTokenSignaler changeTokenSignaler,
    IOptions<CachingOptions> cachingOptions) : IBookmarkStore
{
    private static readonly string CacheInvalidationTokenKey = typeof(CachingBookmarkStore).FullName!;

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        await changeTokenSignaler.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        await decoratedStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken)
    {
        await changeTokenSignaler.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        await decoratedStore.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<StoredBookmark?> FindAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return (await GetOrCreateAsync(cacheKey, async () => await decoratedStore.FindAsync(filter, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = hasher.Hash(filter);
        return (await GetOrCreateAsync(cacheKey, async () => await decoratedStore.FindManyAsync(filter, cancellationToken)))!;
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        await changeTokenSignaler.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        return await decoratedStore.DeleteAsync(filter, cancellationToken);
    }

    private async ValueTask<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory)
    {
        var cacheEntry = await memoryCache.GetOrCreateAsync(key, async entry =>
        {
            var invalidationRequestToken = changeTokenSignaler.GetToken(CacheInvalidationTokenKey);
            entry.AddExpirationToken(invalidationRequestToken);
            entry.SetAbsoluteExpiration(cachingOptions.Value.CacheDuration);
            return await factory();
        });

        return cacheEntry!;
    }
}