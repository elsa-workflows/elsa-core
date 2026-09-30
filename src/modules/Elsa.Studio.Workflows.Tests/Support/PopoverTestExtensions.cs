using AngleSharp.Dom;
using Bunit;
using MudBlazor;

namespace Elsa.Studio.Workflows.Tests.Support;

internal static class PopoverTestExtensions
{
    /// <summary>
    /// Clicks a menu's activator and waits for its items, since <see cref="MudMenu"/> opens its popover asynchronously.
    /// </summary>
    public static IReadOnlyList<IElement> OpenMenu(this IRenderedComponent<MudPopoverProvider> popovers, IElement activator)
    {
        activator.Click();
        return popovers.WaitForElements(".mud-menu-item");
    }
}
