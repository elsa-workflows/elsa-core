using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides default menu group services.
/// </summary>
public class DefaultMenuGroupProvider : IMenuGroupProvider
{
    /// <summary>
    /// Provides the get menu groups async.
    /// </summary>
    public ValueTask<IEnumerable<MenuItemGroup>> GetMenuGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = new List<MenuItemGroup>
        {
            MenuItemGroups.General,
            MenuItemGroups.Diagnostics,
            MenuItemGroups.Administration
        };

        return new(groups);
    }
}
