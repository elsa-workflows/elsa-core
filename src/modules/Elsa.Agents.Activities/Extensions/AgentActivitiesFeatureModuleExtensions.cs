using Elsa.Agents.Activities.Features;
using Elsa.Extensions;
using Elsa.Features.Services;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Agents;

/// <summary>
/// An extension class that installs the Agents feature.
/// </summary>
[PublicAPI]
public static class AgentActivitiesFeatureModuleExtensions
{
    /// <summary>
    /// Installs the Agents feature.
    /// </summary>
    public static IModule UseAgentActivities(this IModule module, Action<AgentActivitiesFeature>? configure = null)
    {
        return module.Use(configure);
    }
}