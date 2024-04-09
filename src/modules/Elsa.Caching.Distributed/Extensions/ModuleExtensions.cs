using Elsa.Caching.Distributed.Features;
using Elsa.Caching.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides methods to install and configure the distributed caching feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the <see cref="DistributedCacheFeature"/> feature to the system.
    /// </summary>
    public static MemoryCacheFeature UseMemoryCache(this IModule module, Action<MemoryCacheFeature>? configure = default)
    {
        return module.Configure(configure);
    }
    
    /// <summary>
    /// Adds the <see cref="DistributedCacheFeature"/> feature to the system.
    /// </summary>
    public static DistributedCacheFeature UseDistributedCache(this MemoryCacheFeature memoryCacheFeature, Action<DistributedCacheFeature>? configure = default)
    {
        return memoryCacheFeature.Module.Configure(configure);
    }
    
    /// <summary>
    /// Adds the <see cref="DistributedCacheFeature"/> feature to the system.
    /// </summary>
    public static IModule UseDistributedCache(this IModule module, Action<DistributedCacheFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}