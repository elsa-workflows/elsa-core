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
    /// Lists the pages the navigation leads to, in display order: every leaf that has an app-relative href. The app root
    /// itself (the landing page) is not among them. Hrefs are relative to the app's base address, so that they stay inside
    /// an app hosted under a sub-path.
    /// </summary>
    public static IEnumerable<MenuPage> GetPages(this IEnumerable<MenuNavigationGroup> navigation) => navigation
        .SelectMany(entry => entry.Items)
        .SelectMany(GetLeaves)
        .Where(item => IsNavigable(item.Href))
        .Select(item => new MenuPage(item.Href.TrimStart('/'), item.Text, item.Icon));

    /// <summary>
    /// Finds the href of the first page the navigation leads to (see <see cref="GetPages"/>).
    /// </summary>
    public static string? FindFirstHref(this IEnumerable<MenuNavigationGroup> navigation) =>
        navigation.GetPages().FirstOrDefault()?.Href;

    private static IEnumerable<MenuItem> GetLeaves(MenuItem item) =>
        item.SubMenuItems.Count > 0 ? item.SubMenuItems.SelectMany(GetLeaves) : [item];

    // Only app-relative targets with a path: not blank, not external or absolute, and not resolving to the app root
    // itself (such as "/", "#" or "?x"), which is the landing page.
    private static bool IsNavigable(string? href) =>
        !string.IsNullOrWhiteSpace(href)
        && Uri.TryCreate(AppRoot, href, out var resolved)
        && resolved.Host == AppRoot.Host
        && resolved.Scheme == AppRoot.Scheme
        && resolved.AbsolutePath.Trim('/').Length > 0;
}
