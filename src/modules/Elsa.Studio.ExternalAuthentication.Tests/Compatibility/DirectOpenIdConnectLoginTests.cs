using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Controllers;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;
using Elsa.Studio.ExternalAuthentication.Tests.Login;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
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
        var result = Assert.IsType<ChallengeResult>(new AuthenticationController().Login(returnUrl));

        Assert.NotNull(result.Properties);
        Assert.Equal(expectedReturnUrl, result.Properties!.RedirectUri);
    }

    [Theory]
    [MemberData(nameof(LocalReturnPathCorpus.Cases), MemberType = typeof(LocalReturnPathCorpus))]
    public void DirectOpenIdConnectSignOutAcceptsOnlyLocalReturnUrls(string? returnUrl, string expectedReturnUrl)
    {
        var result = Assert.IsType<SignOutResult>(new AuthenticationController().Logout(returnUrl));

        Assert.NotNull(result.Properties);
        Assert.Equal(expectedReturnUrl, result.Properties!.RedirectUri);
    }

    [Fact]
    public void DirectOpenIdConnectChallengeRejectsBoundControlCharacterReturnUrls()
    {
        var result = Assert.IsType<ChallengeResult>(new AuthenticationController().Login("/\t/evil.com"));

        Assert.NotNull(result.Properties);
        Assert.Equal("/", result.Properties!.RedirectUri);
    }

    [Fact]
    public void LegacyOidcStateHelperKeepsPathBase()
    {
        var current = "https://studio.example/elsa/studio/workflows/definitions?x=1";

        Assert.Equal("/elsa/studio/workflows/definitions?x=1", OpenIdConnectAuthorizationService.CaptureReturnPath(current));
    }
}
