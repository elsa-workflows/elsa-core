using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.AI.Menu;

/// <summary>
/// Exposes the Weaver workspace menu item.
/// </summary>
public class WeaverMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatureProvider.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        IEnumerable<MenuItem> menuItems =
        [
            new()
            {
                Icon = Icons.Material.Filled.AutoAwesome,
                Href = "ai/weaver",
                Text = "Weaver",
                GroupName = MenuItemGroups.General.Name,
                RequiredPermissions = { new(AIPermissions.Capabilities, PermissionVerbs.View), new(AIPermissions.Tools, PermissionVerbs.View), new(AIPermissions.Chat, PermissionVerbs.Execute) }
            }
        ];

        return menuItems;
    }
}
