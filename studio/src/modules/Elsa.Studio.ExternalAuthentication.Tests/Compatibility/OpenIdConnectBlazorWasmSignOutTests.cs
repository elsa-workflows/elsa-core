using System.Text.Json;
using Bunit;
using Bunit.TestDoubles;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Components;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// On Blazor WebAssembly the OpenID Connect user menu hands sign-out to the framework's remote authenticator, which
/// performs RP-initiated logout when the provider advertises an end_session_endpoint and a local sign-out otherwise.
/// </summary>
public sealed class OpenIdConnectBlazorWasmSignOutTests
    : OpenIdConnectUserMenuTests<OpenIdConnectBlazorWasmFeature, OpenIdConnectUserMenu>
{
    public OpenIdConnectBlazorWasmSignOutTests() => Services.AddOpenIdConnectAuth(ConfigureIdentityProvider);

    [Fact]
    public void SignOut_StartsTheRemoteSignOutAndReturnsToStudio()
    {
        SignIn();
        var menu = RenderAppBarMenu();

        var signOut = FindInOpenMenu(menu, ".mud-menu-item");
        Assert.Equal("Sign out", signOut.TextContent.Trim());
        signOut.Click();

        var history = Services.GetRequiredService<BunitNavigationManager>().History;
        menu.WaitForAssertion(() => Assert.NotEmpty(history));
        var navigation = Assert.Single(history);
        var request = JsonSerializer.Deserialize<InteractiveRequestOptions>(Assert.IsType<string>(navigation.Options.HistoryEntryState))!;
        Assert.Equal("authentication/logout", navigation.Uri);
        Assert.Equal(InteractionType.SignOut, request.Interaction);
        Assert.Equal("http://localhost/", request.ReturnUrl);
    }

    [Fact]
    public void Provider_RedirectsBackToStudioAfterSignOut()
    {
        using var scope = Services.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>().Value;

        Assert.Equal("http://localhost/authentication/logout-callback", options.ProviderOptions.PostLogoutRedirectUri);
    }
}
