using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.Abstractions.Options;
using Elsa.Studio.Authentication.Abstractions.Validation;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

public class AuthenticationModeTests
{
    [Fact]
    public void DirectOpenIdConnectRemainsValidWhenItIsTheOnlyRegisteredHandler()
    {
        var result = Validate(
            StudioAuthenticationProvider.OpenIdConnect,
            StudioAuthenticationProvider.OpenIdConnect);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void BrokeredModeIsValidOnlyWhenExplicitlySelected()
    {
        var result = Validate(
            StudioAuthenticationProvider.ExternalAuthentication,
            StudioAuthenticationProvider.ExternalAuthentication);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(StudioAuthenticationProvider.ElsaIdentity)]
    [InlineData(StudioAuthenticationProvider.ElsaLogin)]
    public void LocalOnlyModesRemainValidWhenTheirOwnIntegrationIsRegistered(
        StudioAuthenticationProvider provider)
    {
        var result = Validate(provider, provider);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void DirectAndBrokeredHandlersFailStartupEvenWhenOneIsSelected()
    {
        var result = Validate(
            StudioAuthenticationProvider.ExternalAuthentication,
            StudioAuthenticationProvider.OpenIdConnect,
            StudioAuthenticationProvider.ExternalAuthentication);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("Conflicting", StringComparison.Ordinal));
    }

    [Fact]
    public void DeprecatedLoginAndBrokerShellFailStartupWhenRegisteredTogether()
    {
        var result = Validate(
            StudioAuthenticationProvider.ExternalAuthentication,
            StudioAuthenticationProvider.ElsaLogin,
            StudioAuthenticationProvider.ExternalAuthentication);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("ElsaLogin", StringComparison.Ordinal) &&
            failure.Contains("ExternalAuthentication", StringComparison.Ordinal));
    }

    [Fact]
    public void SelectedBrokerModeFailsWhenOnlyDirectHandlersAreRegistered()
    {
        var result = Validate(
            StudioAuthenticationProvider.ExternalAuthentication,
            StudioAuthenticationProvider.OpenIdConnect);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("selects", StringComparison.Ordinal) &&
                       failure.Contains("OpenIdConnect", StringComparison.Ordinal));
    }

    private static Microsoft.Extensions.Options.ValidateOptionsResult Validate(
        StudioAuthenticationProvider selected,
        params StudioAuthenticationProvider[] registered)
    {
        var validator = new StudioAuthenticationOptionsValidator(
            registered.Select(x => new StudioAuthenticationProviderRegistration(x)));
        return validator.Validate(null, new StudioAuthenticationOptions { Provider = selected });
    }
}
