using Elsa.Caching.Distributed.Features;
using Elsa.Caching.Distributed.MassTransit.Features;
using Elsa.Caching.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides methods to install and configure the distributed caching feature with MassTransit.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Configures the distributed caching feature to use MassTransit.
    /// </summary>
    public static MassTransitDistributedCacheFeature UseMassTransit(this DistributedCacheFeature distributedCacheFeature, Action<MassTransitDistributedCacheFeature>? configure = default)
    {
        return distributedCacheFeature.Module.Configure(configure);
    }

    /// <summary>
    /// Configures the memory caching feature with the distributed caching feature that uses MassTransit.
    /// </summary>
    public static MemoryCacheFeature UseMassTransit(this MemoryCacheFeature memoryCacheFeature, Action<MassTransitDistributedCacheFeature>? configure = default)
    {
        memoryCacheFeature.Module.Configure(configure);
        return memoryCacheFeature;
    }
}