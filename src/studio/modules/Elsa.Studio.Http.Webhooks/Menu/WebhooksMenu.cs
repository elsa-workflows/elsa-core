using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Http.Webhooks.Menu;

public class WebhooksMenu(ILocalizer localizer) : IMenuProvider
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Http,
                Href = "webhooks",
                Text = localizer["Webhooks"],
                GroupName = MenuItemGroups.Administration.Name
            }
        };

        return new(menuItems);
    }
}
