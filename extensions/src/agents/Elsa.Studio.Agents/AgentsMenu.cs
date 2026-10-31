using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;

namespace Elsa.Studio.Agents;

/// A menu provider for the Agents module.
public class AgentsMenu(ILocalizer localizer, IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatureProvider.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        return
        [
            new MenuItem
            {
                Icon = AgentIcons.Robot,
                Href = "ai/agents",
                Text = localizer["Agents"],
                GroupName = MenuItemGroups.General.Name,
                RequiredPermissions = { new("ai/agents", "read") }
            }
        ];
    }
}
