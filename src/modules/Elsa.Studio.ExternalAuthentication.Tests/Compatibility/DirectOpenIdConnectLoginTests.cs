using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Controllers;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;
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
    [InlineData("https://attacker.example", "/")]
    [InlineData("http://attacker.example", "/")]
    [InlineData("//evil.com", "/")]
    [InlineData("/\\evil.com", "/")]
    [InlineData("\\\\evil.com", "/")]
    [InlineData("/%09/evil.com", "/")]
    [InlineData("/%2F/evil.com", "/")]
    [InlineData("/workflows?version=1", "/workflows?version=1")]
    [InlineData("/workflows/definitions?x=1", "/workflows/definitions?x=1")]
    public void DirectOpenIdConnectChallengeAcceptsOnlyLocalReturnUrls(string returnUrl, string expectedReturnUrl)
    {
        var result = Assert.IsType<ChallengeResult>(new AuthenticationController().Login(returnUrl));

        Assert.NotNull(result.Properties);
        Assert.Equal(expectedReturnUrl, result.Properties!.RedirectUri);
    }

    [Theory]
    [InlineData("/%09/evil.com", "/")]
    [InlineData("//evil.com", "/")]
    [InlineData("/\\evil.com", "/")]
    [InlineData("/workflows/definitions?x=1", "/workflows/definitions?x=1")]
    public void DirectOpenIdConnectSignOutAcceptsOnlyLocalReturnUrls(string returnUrl, string expectedReturnUrl)
    {
        var result = Assert.IsType<SignOutResult>(new AuthenticationController().Logout(returnUrl));

        Assert.NotNull(result.Properties);
        Assert.Equal(expectedReturnUrl, result.Properties!.RedirectUri);
    }
}
