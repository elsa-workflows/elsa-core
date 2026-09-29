using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultMenuService : IMenuService
{
    private readonly IEnumerable<IMenuProvider> _menuProviders;
    private readonly IEnumerable<IMenuGroupProvider> _menuGroupProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMenuService"/> class.
    /// </summary>
    public DefaultMenuService(IEnumerable<IMenuProvider> menuProviders, IEnumerable<IMenuGroupProvider> menuGroupProviders)
    {
        _menuProviders = menuProviders;
        _menuGroupProviders = menuGroupProviders;
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

        return menu.OrderBy(x => x.Order).ToList();
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
}