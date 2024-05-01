using Elsa.Caching;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// A decorator for <see cref="IBookmarkStore"/> that caches bookmark records.
/// </summary>
public class CachingBookmarkStore(IBookmarkStore decoratedStore, ICacheManager cacheManager, IHasher hasher) : IBookmarkStore
{
    private static readonly string CacheInvalidationTokenKey = typeof(CachingBookmarkStore).FullName!;

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        await decoratedStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken)
    {
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
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
        await cacheManager.TriggerTokenAsync(CacheInvalidationTokenKey, cancellationToken);
        return await decoratedStore.DeleteAsync(filter, cancellationToken);
    }

    private async ValueTask<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory)
    {
        var cacheEntry = await cacheManager.GetOrCreateAsync(key, async entry =>
        {
            var invalidationRequestToken = cacheManager.GetToken(CacheInvalidationTokenKey);
            entry.AddExpirationToken(invalidationRequestToken);
            entry.SetAbsoluteExpiration(cacheManager.CachingOptions.Value.CacheDuration);
            return await factory();
        });

        return cacheEntry!;
    }
}