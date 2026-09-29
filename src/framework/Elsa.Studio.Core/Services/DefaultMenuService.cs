using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultMenuService : IMenuService
{
    private readonly IEnumerable<IMenuProvider> _menuProviders;
    private readonly IEnumerable<IMenuGroupProvider> _menuGroupProviders;
    private readonly IPermissionService? _permissionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMenuService"/> class.
    /// </summary>
    public DefaultMenuService(IEnumerable<IMenuProvider> menuProviders, IEnumerable<IMenuGroupProvider> menuGroupProviders, IPermissionService? permissionService = null)
    {
        _menuProviders = menuProviders;
        _menuGroupProviders = menuGroupProviders;
        _permissionService = permissionService;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menu = new List<MenuItem>();

        foreach (var menuProvider in _menuProviders)
        {
            var menuItems = await menuProvider.GetMenuItemsAsync(cancellationToken);
            menu.AddRange(menuItems);
        }

        var permissions = _permissionService != null ? await _permissionService.GetPermissionsAsync(cancellationToken) : UserPermissions.Unknown;

        return FilterByPermissions(menu, permissions).OrderBy(x => x.Order).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = new List<MenuItemGroup>();

        foreach (var menuGroupProvider in _menuGroupProviders)
        {
            var menuGroups = await menuGroupProvider.GetMenuGroupsAsync(cancellationToken);
            groups.AddRange(menuGroups);
        }

        return groups.DistinctBy(x => x.Name).OrderBy(x => x.Order).ToList();
    }

    /// <summary>
    /// Removes the items the user lacks permissions for, and parents left without any visible children. Items are
    /// copied rather than mutated, since providers may hand out shared instances.
    /// </summary>
    private static IEnumerable<MenuItem> FilterByPermissions(IEnumerable<MenuItem> items, UserPermissions permissions)
    {
        foreach (var item in items)
        {
            if (!permissions.HasAll(item.RequiredPermissions))
                continue;

            if (item.SubMenuItems.Count == 0)
            {
                yield return item;
                continue;
            }

            var subMenuItems = FilterByPermissions(item.SubMenuItems, permissions).ToList();

            if (subMenuItems.Count > 0)
                yield return item.WithSubMenuItems(subMenuItems);
        }
    }
}
