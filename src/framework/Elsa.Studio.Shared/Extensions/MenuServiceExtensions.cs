using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Contains extension methods for <see cref="IMenuService" /> that arrange the navigation menu in display order: by
/// group, then by item within the group. Shared by everything that needs to agree with what the navigation shows.
/// </summary>
public static class MenuServiceExtensions
{
    // Hrefs are resolved against a stand-in for the app root, to tell app-relative targets from everything else.
    private static readonly Uri AppRoot = new("http://app.invalid/");

    /// <summary>Loads the menu and arranges it as the navigation shows it. Groups without items are omitted.</summary>
    public static async Task<IReadOnlyList<MenuNavigationGroup>> GetNavigationAsync(this IMenuService menuService, CancellationToken cancellationToken = default)
    {
        var groups = (await menuService.GetMenuItemGroupsAsync(cancellationToken)).ToDictionary(x => x.Name);
        var items = (await menuService.GetMenuItemsAsync(cancellationToken)).ToList();

        return groups.Values
            .Select(group => new MenuNavigationGroup(group, items.Where(item => item.GroupName == group.Name).ToList()))
            .Where(entry => entry.Items.Count > 0)
            .ToList();
    }

    /// <summary>
    /// Finds the first page the navigation leads to: the first leaf in display order that has an app-relative href,
    /// returned relative to the app's base address so that it stays inside an app hosted under a sub-path.
    /// </summary>
    public static string? FindFirstHref(this IEnumerable<MenuNavigationGroup> navigation) => navigation
        .SelectMany(entry => entry.Items)
        .Select(FindFirstHref)
        .FirstOrDefault(href => href != null);

    private static string? FindFirstHref(MenuItem item) => item.SubMenuItems.Count > 0
        ? item.SubMenuItems.Select(FindFirstHref).FirstOrDefault(href => href != null)
        : IsNavigable(item.Href) ? item.Href.TrimStart('/') : null;

    // Only app-relative targets with a path: not blank, not external or absolute, and not resolving to the app root
    // itself (such as "/", "#" or "?x"), which is the page the user is being sent away from.
    private static bool IsNavigable(string? href) =>
        !string.IsNullOrWhiteSpace(href)
        && Uri.TryCreate(AppRoot, href, out var resolved)
        && resolved.Host == AppRoot.Host
        && resolved.Scheme == AppRoot.Scheme
        && resolved.AbsolutePath.Trim('/').Length > 0;
}
