using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Testing;

/// <summary>
/// Asserts where an app bar menu's popover opens relative to its button.
/// </summary>
internal static class MenuPopoverAssert
{
    /// <summary>Renders <typeparamref name="TMenu"/>, opens it, and asserts its popover drops down below its button.</summary>
    public static void OpensBelowItsButton<TMenu>(BunitContext context) where TMenu : IComponent
    {
        var popoverProvider = context.Render<MudPopoverProvider>();

        context.Render<TMenu>().Find(".mud-menu button").Click();

        OpensBelowItsButton(popoverProvider.WaitForElement(".mud-popover"));
    }

    /// <summary>
    /// Asserts the popover's top edge sits on the button's bottom edge, rather than its bottom edge covering the button.
    /// </summary>
    public static void OpensBelowItsButton(IElement popover)
    {
        Assert.Contains("mud-popover-anchor-bottom-right", popover.ClassList);
        Assert.Contains("mud-popover-top-right", popover.ClassList);
    }
}
