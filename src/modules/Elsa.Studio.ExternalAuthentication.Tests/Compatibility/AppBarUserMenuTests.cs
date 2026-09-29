using AngleSharp.Dom;
using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// The harness for the app bar user menu each authentication provider contributes, shown only to a signed-in user.
/// </summary>
public abstract class AppBarUserMenuTests<TFeature, TMenu> : BunitContext, IAsyncLifetime
    where TFeature : IFeature
    where TMenu : IComponent
{
    protected const string UserName = "alice";

    protected AppBarUserMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddScoped<IAppBarService, DefaultAppBarService>();
    }

    protected IRenderedComponent<MudPopoverProvider>? PopoverProvider { get; private set; }

    // The shell initializes features before it renders anything.
    Task IAsyncLifetime.InitializeAsync() => Services.GetServices<IFeature>().OfType<TFeature>().Single().InitializeAsync().AsTask();
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void SignedInUser_SeesTheirNameInTheAppBar()
    {
        SignIn();

        var menu = RenderAppBarMenu();

        menu.WaitForAssertion(() => Assert.Contains(UserName, menu.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void AnonymousUser_SeesNoUserMenu()
    {
        var menu = RenderAppBarMenu();

        Assert.Empty(menu.FindAll(".mud-menu"));
    }

    protected abstract void SignIn();

    /// <summary>Renders the app bar component the provider's feature contributed, the way the shell does.</summary>
    protected IRenderedComponent<TMenu> RenderAppBarMenu()
    {
        var element = Assert.Single(Services.GetRequiredService<IAppBarService>().AppBarElements);
        PopoverProvider = Render<MudPopoverProvider>();
        return Render(element.Component).FindComponent<TMenu>();
    }

    protected IElement FindInOpenMenu(IRenderedComponent<TMenu> menu, string selector)
    {
        menu.Find(".mud-menu button").Click();
        return PopoverProvider!.WaitForElement(selector);
    }
}
