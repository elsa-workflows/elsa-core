using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Controllers;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;
using Elsa.Studio.ExternalAuthentication.Tests.Login;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

public class DirectOpenIdConnectLoginTests
{
    [Fact]
    public async Task DirectOpenIdConnectContributesItsLegacyChallengeToTheSharedLoginShell()
    {
        var result = await new DirectOpenIdConnectLoginMethodCatalog().ListAsync();

        var method = Assert.Single(result.Methods);
        Assert.Equal("direct-openid-connect", method.Kind);
        Assert.Equal("/authentication/login", method.InitiationUri);
    }

    [Theory]
    [MemberData(nameof(LocalReturnPathCorpus.Cases), MemberType = typeof(LocalReturnPathCorpus))]
    public void DirectOpenIdConnectChallengeAcceptsOnlyLocalReturnUrls(string? returnUrl, string expectedReturnUrl)
    {
        var result = Assert.IsType<ChallengeResult>(CreateController().Login(returnUrl));

        Assert.NotNull(result.Properties);
        Assert.Equal(expectedReturnUrl, result.Properties!.RedirectUri);
    }

    [Theory]
    [MemberData(nameof(LocalReturnPathCorpus.Cases), MemberType = typeof(LocalReturnPathCorpus))]
    public void DirectOpenIdConnectSignOutAcceptsOnlyLocalReturnUrls(string? returnUrl, string expectedReturnUrl)
    {
        var result = Assert.IsType<SignOutResult>(CreateController().Logout(returnUrl));

        Assert.NotNull(result.Properties);
        Assert.Equal(expectedReturnUrl, result.Properties!.RedirectUri);
    }

    [Fact]
    public void DirectOpenIdConnectChallengeRejectsBoundControlCharacterReturnUrls()
    {
        var result = Assert.IsType<ChallengeResult>(CreateController().Login("/\t/evil.com"));

        Assert.NotNull(result.Properties);
        Assert.Equal("/", result.Properties!.RedirectUri);
    }

    [Fact]
    public void LegacyOidcStateHelperKeepsPathBase()
    {
        var current = "https://studio.example/elsa/studio/workflows/definitions?x=1";

        Assert.Equal("/elsa/studio/workflows/definitions?x=1", OpenIdConnectAuthorizationService.CaptureReturnPath(current));
    }

    private static AuthenticationController CreateController()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new AuthenticationController
        {
            ControllerContext = new ControllerContext(actionContext),
            Url = new UrlHelper(actionContext)
        };
    }
}
