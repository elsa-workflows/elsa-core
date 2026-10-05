using Bunit;
using Bunit.TestDoubles;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// Each OpenID Connect hosting model contributes the shared app bar user menu, shown only to a signed-in user.
/// </summary>
public abstract class OpenIdConnectUserMenuTests<TFeature, TMenu> : AppBarUserMenuTests<TFeature, TMenu>
    where TFeature : IFeature
    where TMenu : IComponent
{
    private readonly BunitAuthorizationContext _authorization;

    protected OpenIdConnectUserMenuTests() => _authorization = AddAuthorization();

    protected static void ConfigureIdentityProvider(OidcOptions options)
    {
        options.Authority = "https://idp.example";
        options.ClientId = "elsa-studio";
    }

    protected override void SignIn() => _authorization.SetAuthorized(UserName);
}
