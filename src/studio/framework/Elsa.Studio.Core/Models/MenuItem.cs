using Microsoft.AspNetCore.Components.Routing;

namespace Elsa.Studio.Models;

/// <summary>
/// A menu item.
/// </summary>
public class MenuItem
{
    /// <summary>
    /// The icon to use in SVG format.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The path to navigate to.
    /// </summary>
    public string Href { get; set; } = default!;

    /// <summary>
    /// The <see cref="NavLinkMatch"/> to use.
    /// </summary>
    public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    /// <summary>
    /// The text to display.
    /// </summary>
    public string Text { get; set; } = default!;

    /// <summary>
    /// The order of the menu item.
    /// </summary>
    public float Order { get; set; }

    /// <summary>
    /// A list of sub menu items.
    /// </summary>
    public ICollection<MenuItem> SubMenuItems { get; set; } = new List<MenuItem>();

    /// <summary>
    /// The name of the group this menu item belongs to.
    /// </summary>
    public string GroupName { get; set; } = "General";
}