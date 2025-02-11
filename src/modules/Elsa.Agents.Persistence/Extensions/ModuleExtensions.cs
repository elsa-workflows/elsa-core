using Elsa.Agents.Persistence.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the persistence feature for the Agents module.
    /// </summary>
    public static IModule UseAgentPersistence(this IModule module, Action<AgentPersistenceFeature>? configure = null)
    {
        return module.Use(configure);
    }
}