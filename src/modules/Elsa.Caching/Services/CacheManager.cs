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
    public async Task<TItem?> GetOrCreateAsync<TItem>(object key, Func<ICacheEntry, Task<TItem>> factory)
    {
        return await memoryCache.GetOrCreateAsync(key, async entry => await factory(entry));
    }
}