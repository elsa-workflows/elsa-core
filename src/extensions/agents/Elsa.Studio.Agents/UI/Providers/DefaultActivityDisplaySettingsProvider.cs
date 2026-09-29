using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using JetBrains.Annotations;

namespace Elsa.Studio.Agents.UI.Providers;

/// <summary>
/// Provides default activity display settings.
/// </summary>
[UsedImplicitly]
public class AgentsActivityDisplaySettingsProvider(IActivityRegistry activityRegistry) : IActivityDisplaySettingsProvider
{
    /// <inheritdoc />
    public IDictionary<string, ActivityDisplaySettings> GetSettings()
    {
        var agentActivityDescriptors = activityRegistry.List()
            .Where(x => x.CustomProperties.TryGetValue("RootType", out var rootType) && rootType.ToString() == "AgentActivity")
            .ToList();
        
        return agentActivityDescriptors.ToDictionary(x => x.TypeName, x => new ActivityDisplaySettings("#1ec7b9", AgentIcons.Robot));
    }
}