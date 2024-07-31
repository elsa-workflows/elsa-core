using Elsa.Agents.Features;
using Elsa.Features.Services;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// An extension class that installs the Agents feature.
[PublicAPI]
public static class AgentActivitiesFeatureModuleExtensions
{
    /// Installs the Agents feature.
    public static IModule UseAgents(this IModule module, Action<AgentsFeature>? configure = null)
    {
        return module.Use(configure);
    }
}