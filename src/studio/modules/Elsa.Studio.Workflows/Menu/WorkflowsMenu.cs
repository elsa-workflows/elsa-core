using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Services;
using MudBlazor;

namespace Elsa.Studio.Workflows.Menu;

/// <summary>
/// Exposes menu entries for workflows.
/// </summary>
public class WorkflowsMenu(ILocalizer localizer, IEnumerable<IWorkflowMenuContributor> contributors) : IMenuProvider
{
    /// <summary>
    /// Provides the get menu items async.
    /// </summary>
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var workflowItem = new MenuItem
        {
            Icon = Icons.Material.Outlined.Schema,
            Text = localizer["Workflows"],
            GroupName = MenuItemGroups.General.Name,
            SubMenuItems =
            {
                new MenuItem()
                {
                    Text = localizer["Definitions"],
                    Href = "workflows/definitions"
                },
                new MenuItem()
                {
                    Text = localizer["Instances"],
                    Href = "workflows/instances"
                },
            }
        };

        foreach (var contributor in contributors)
        {
            var items = await contributor.GetWorkflowMenuItemsAsync(cancellationToken);
            foreach (var item in items)
                workflowItem.SubMenuItems.Add(item);
        }

        var menuItems = new List<MenuItem>
        {
            workflowItem
        };

        return menuItems;
    }
}
