using Elsa.Agents.Activities.Features;
using Elsa.Features.Services;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// An extension class that installs the Agents feature.
[PublicAPI]
public static class AgentActivitiesFeatureModuleExtensions
{
    /// Installs the Agents feature.
    public static IModule UseAgentActivities(this IModule module, Action<AgentActivitiesFeature>? configure = null)
    {
        return module.Use(configure);
    }
}