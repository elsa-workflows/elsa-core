using Elsa.Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.Caching.Services;

/// <inheritdoc />
public class CacheManager(IMemoryCache memoryCache, IChangeTokenSignaler changeTokenSignaler, IOptions<CachingOptions> options) : ICacheManager
{
    /// <inheritdoc />
    public IOptions<CachingOptions> CachingOptions => options;

    /// <inheritdoc />
    public IChangeTokenSignaler ChangeTokenSignaler => changeTokenSignaler;

    /// <inheritdoc />
    public async Task<TItem?> GetOrCreateAsync<TItem>(object key, Func<ICacheEntry, Task<TItem>> factory)
    {
        return await memoryCache.GetOrCreateAsync(key, async entry => await factory(entry));
    }
}