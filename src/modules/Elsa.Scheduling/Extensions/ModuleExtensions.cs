using Elsa.Features.Services;
using Elsa.Scheduling.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IModule"/> instances.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enables the scheduling feature.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="configure">A delegate to configure the feature.</param>
    /// <returns>The module.</returns>
    public static IModule UseScheduling(this IModule module, Action<SchedulingFeature>? configure = default )
    {
        module.Configure(configure);
        return module;
    }
}