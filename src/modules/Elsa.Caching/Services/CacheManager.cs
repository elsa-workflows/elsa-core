using Elsa.Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Services;

/// <inheritdoc />
public class CacheManager(IMemoryCache memoryCache, IChangeTokenSignaler changeTokenSignaler, IOptions<CachingOptions> options) : ICacheManager
{
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
    public async Task<TItem?> FindOrCreateAsync<TItem>(object key, Func<ICacheEntry, Task<TItem>> factory)
    {
        return await memoryCache.GetOrCreateAsync(key, async entry => await factory(entry));
    }

    /// <summary>
    /// Retrieves a cached item by the specified key or creates a new one using the provided factory function.
    /// </summary>
    /// <param name="key">The key used to identify the cached item.</param>
    /// <param name="factory">A factory function that provides the value to be cached if it does not already exist.</param>
    /// <typeparam name="TItem">The type of the item to retrieve or create.</typeparam>
    /// <returns>The cached or newly created item.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the factory function returns null.</exception>
    public async Task<TItem> GetOrCreateAsync<TItem>(object key, Func<ICacheEntry, Task<TItem>> factory)
    {
        return await memoryCache.GetOrCreateAsync(key, async entry => await factory(entry)) ?? throw new InvalidOperationException("Factory returned null.");
    }
}