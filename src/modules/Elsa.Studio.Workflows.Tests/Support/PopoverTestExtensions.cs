using AngleSharp.Dom;
using Bunit;
using MudBlazor;

namespace Elsa.Studio.Workflows.Tests.Support;

internal static class PopoverTestExtensions
{
    /// <summary>
    /// Clicks a menu's activator and waits for that menu's items, since <see cref="MudMenu"/> opens its popover asynchronously.
    /// </summary>
    public static IReadOnlyList<IElement> OpenMenu(this IRenderedComponent<MudPopoverProvider> popovers, IElement activator)
    {
        // The menu holds a "popover-{id}" placeholder; the provider renders its items under "popovercontent-{id}".
        var popoverId = activator.Closest(".mud-menu")!.QuerySelector("[id^='popover-']")!.Id!["popover-".Length..];
        activator.Click();
        return popovers.WaitForElements($"[id='popovercontent-{popoverId}'] .mud-menu-item");
    }
}
