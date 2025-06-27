using Elsa.Features.Services;
using Elsa.IO.Http.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to install the <see cref="IOHttpFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Install the <see cref="IOHttpFeature"/> feature.
    /// </summary>
    public static IModule UseIOHttp(this IModule module, Action<IOHttpFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}