using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Labels.Menu;

/// <summary>
/// Provides the menu items for the Labels module.
/// </summary>
public class LabelsMenu(ILocalizer localizer, IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
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
                Icon = Icons.Material.Filled.Label,
                Href = "Labels",
                Text = localizer["Labels"],
                GroupName = MenuItemGroups.Administration.Name,
                Order = 200
            }
        ];
    }
}
