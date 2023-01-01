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
}