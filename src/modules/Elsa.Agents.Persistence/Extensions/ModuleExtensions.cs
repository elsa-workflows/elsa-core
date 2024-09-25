using Elsa.Agents.Persistence.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.  
public static class ModuleExtensions
{
    /// Installs the persistence feature for the Agents module.
    public static IModule UseAgentPersistence(this IModule module, Action<AgentPersistenceFeature>? configure = null)
    {
        return module.Use(configure);
    }
}