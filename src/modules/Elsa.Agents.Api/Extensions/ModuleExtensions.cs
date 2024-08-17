using Elsa.Agents.Api.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.  
public static class ModuleExtensions
{
    /// Installs the Semantic Kernel API feature.
    public static IModule UseAgentsApi(this IModule module, Action<AgentsApiFeature>? configure = null)
    {
        return module.Use(configure);
    }
}