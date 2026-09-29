using Elsa.Studio.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// A service that provides menu items and menu item groups.
/// </summary>
public interface IMenuService
{
    /// <summary>
    /// Returns all menu items from all menu providers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="MenuItem"/> instances.</returns>
    ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns all menu item groups from all menu group providers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="MenuItemGroup"/> instances.</returns>
    ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default);
}