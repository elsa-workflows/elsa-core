using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;

namespace Elsa.Studio.Agents;

/// A menu provider for the Agents module.
public class AgentsMenu(ILocalizer localizer) : IMenuProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = AgentIcons.Robot,
                Href = "ai/agents",
                Text = localizer["Agents"],
                GroupName = MenuItemGroups.General.Name
            }
        };

        return new(menuItems);
    }
}