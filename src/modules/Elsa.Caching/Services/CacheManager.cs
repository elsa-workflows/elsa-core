using System.Collections.Concurrent;
using Elsa.Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Services;

/// <inheritdoc />
public class CacheManager(IMemoryCache memoryCache, IChangeTokenSignaler changeTokenSignaler, IOptions<CachingOptions> options) : ICacheManager
{
    private readonly ConcurrentDictionary<object, SemaphoreSlim> _keyLocks = new();

    /// <inheritdoc />
    public IOptions<CachingOptions> CachingOptions => options;

    /// <inheritdoc />
    public IChangeToken GetToken(string key)
    {
        return changeTokenSignaler.GetToken(key);
    }

    /// <inheritdoc />
    public ValueTask TriggerTokenAsync(string key, CancellationToken cancellationToken = default)
    {
        return changeTokenSignaler.TriggerTokenAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TItem?> GetOrCreateAsync<TItem>(object key, Func<ICacheEntry, Task<TItem>> factory)
    {
        // Get or create a semaphore for this specific key
        var keyLock = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await keyLock.WaitAsync();
        try
        {
            return await memoryCache.GetOrCreateAsync(key, async entry => await factory(entry));
        }
        finally
        {
            keyLock.Release();

            // Clean up the semaphore if the cache entry was removed
            if (!memoryCache.TryGetValue(key, out _))
            {
                _keyLocks.TryRemove(key, out _);
            }
        }
    }
}