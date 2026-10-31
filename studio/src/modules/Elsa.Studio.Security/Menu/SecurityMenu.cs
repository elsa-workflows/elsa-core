using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Contracts;
using MudBlazor;

namespace Elsa.Studio.Security.Menu;

/// <summary>
/// Provides menu items for the security module, including users and roles management.
/// </summary>
public class SecurityMenu(IEnumerable<ISecurityMenuContributor> contributors) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var children = new List<MenuItem>();
        foreach (var contributor in contributors)
            children.AddRange(await contributor.GetMenuItemsAsync(cancellationToken));

        children = children
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Text, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Href, StringComparer.Ordinal)
            .ToList();
        if (children.Count == 0)
            return [];

        return
        [
            new()
            {
                Icon = Icons.Material.Filled.ManageAccounts,
                Href = children[0].Href,
                Text = "Identity & access",
                GroupName = MenuItemGroups.Administration.Name,
                Order = 100,
                SubMenuItems = children
            }
        ];
    }
}
