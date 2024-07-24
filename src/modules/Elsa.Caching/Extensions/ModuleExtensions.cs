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
    /// Adds the <see cref="MemoryCacheFeature"/> feature to the system.
    /// </summary>
    public static MemoryCacheFeature UseMemoryCache(this IModule module, Action<MemoryCacheFeature>? configure = default) => module.Configure(configure);
}