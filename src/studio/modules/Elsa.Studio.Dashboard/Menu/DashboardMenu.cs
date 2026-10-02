using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Elsa.Studio.Dashboard.Menu;

/// <summary>
/// Provides the menu items for the Dashboard module.
/// </summary>
public class DashboardMenu(ILocalizer localizer) : IMenuProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Outlined.SpaceDashboard,
                Href = "",
                Text = localizer["Dashboard"],
                GroupName = MenuItemGroups.General.Name,
                Match = NavLinkMatch.All
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}
