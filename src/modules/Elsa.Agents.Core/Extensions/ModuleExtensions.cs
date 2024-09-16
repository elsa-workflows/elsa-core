using Elsa.Agents.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.  
public static class ModuleExtensions
{
    /// Installs the Semantic Kernel API feature.
    public static IModule UseAgents(this IModule module, Action<AgentsFeature>? configure = null)
    {
        return module.Use(configure);
    }
}