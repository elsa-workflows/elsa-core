using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Settings.Contracts;
using MudBlazor;

namespace Elsa.Studio.Settings.Menu;

public sealed class SettingsMenu(ISettingsSectionRegistry registry) : IMenuProvider
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var sections = await registry.ListAsync(cancellationToken);
        if (sections.Count == 0)
            return [];

        var settings = new MenuItem
        {
            Icon = Icons.Material.Filled.Settings,
            Href = "settings",
            Match = Microsoft.AspNetCore.Components.Routing.NavLinkMatch.Prefix,
            Text = "Settings",
            GroupName = MenuItemGroups.Administration.Name,
            Order = 900,
            SubMenuItems =
            {
                new MenuItem
                {
                    Icon = Icons.Material.Outlined.Home,
                    Href = "settings",
                    Match = Microsoft.AspNetCore.Components.Routing.NavLinkMatch.All,
                    Text = "Overview",
                    Order = float.MinValue
                }
            }
        };

        foreach (var section in sections)
        {
            settings.SubMenuItems.Add(new MenuItem
            {
                Icon = section.Icon,
                Href = section.Href,
                Text = section.DisplayName,
                Order = section.Order
            });
        }

        return [settings];
    }
}
