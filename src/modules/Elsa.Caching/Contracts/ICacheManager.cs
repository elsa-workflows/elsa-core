using Elsa.Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.Caching;

/// <summary>
/// A thin wrapper around <see cref="IMemoryCache"/>, allowing for centralized handling of cache entries.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Provides options for configuring caching.
    /// </summary>
    IOptions<CachingOptions> CachingOptions { get; }

    /// <summary>
    /// Provides a mechanism for signaling changes to cache entries, allowing code to evict cache entries by triggering a signal.
    /// </summary>
    IChangeTokenSignaler ChangeTokenSignaler { get; }
    
    /// <summary>
    /// Gets an item from the cache, or creates it if it doesn't exist.
    /// </summary>
    Task<TItem?> GetOrCreateAsync<TItem>(object key, Func<ICacheEntry, Task<TItem>> factory);
}