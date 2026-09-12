using Elsa.Bpmn.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IModule"/> instances.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enables the BPMN feature.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="configure">A delegate to configure the feature.</param>
    /// <returns>The module.</returns>
    public static IModule UseBpmn(this IModule module, Action<BpmnFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
