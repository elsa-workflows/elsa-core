using Elsa.Features.Services;
using Elsa.Http.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to install the <see cref="HttpFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Install the <see cref="HttpFeature"/> feature.
    /// </summary>
    public static IModule UseHttp(this IModule module, Action<HttpFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
    
    /// <summary>
    /// Install the <see cref="HttpCacheFeature"/> feature to speed up HTTP workflows. Like, a lot.
    /// </summary>
    public static HttpFeature UseCache(this HttpFeature feature, Action<HttpCacheFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}