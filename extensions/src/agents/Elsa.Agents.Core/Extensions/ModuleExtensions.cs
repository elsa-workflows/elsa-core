using Elsa.Agents.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the Semantic Kernel API feature.
    /// </summary>
    public static IModule UseAgentsCore(this IModule module, Action<AgentsCoreFeature>? configure = null)
    {
        return module.Use(configure);
    }
}