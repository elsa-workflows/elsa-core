namespace Elsa.Studio.Models;

/// A menu item group with the items that belong to it, in display order.
public record MenuNavigationGroup(MenuItemGroup Group, IReadOnlyList<MenuItem> Items);
