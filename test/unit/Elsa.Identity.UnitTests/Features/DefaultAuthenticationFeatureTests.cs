using Elsa.Features.Services;
using Elsa.Identity.Features;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Features;

public class DefaultAuthenticationFeatureTests
{
    // These tests previously asserted the shape of the SecurityRoot policy. That policy has been retired in
    // favour of endpoint permissions (ADR 0010), so what matters now is that the feature registers no policy
    // of its own and still honours a host's own authorization configuration.

    [Fact]
    public void DefaultAuthorizationConfigurationRegistersNoPolicy()
    {
        var feature = new DefaultAuthenticationFeature(Substitute.For<IModule>());
        var options = new AuthorizationOptions();

        feature.ConfigureAuthorizationOptions(options);

        Assert.Null(options.GetPolicy("SecurityRoot"));
    }

    [Fact]
    public void CustomAuthorizationConfigurationIsHonoured()
    {
        var feature = new DefaultAuthenticationFeature(Substitute.For<IModule>());
        feature.ConfigureAuthorizationOptions = options => options.AddPolicy("Custom", policy => policy.RequireAuthenticatedUser());
        var options = new AuthorizationOptions();

        feature.ConfigureAuthorizationOptions(options);

        Assert.NotNull(options.GetPolicy("Custom"));
        Assert.Null(options.GetPolicy("SecurityRoot"));
    }

    [Fact]
    public void NullConfigureAuthorizationOptionsFallsBackToANoOp()
    {
        var feature = new DefaultAuthenticationFeature(Substitute.For<IModule>())
        {
            ConfigureAuthorizationOptions = null!
        };
        var options = new AuthorizationOptions();

        var exception = Record.Exception(() => feature.ConfigureAuthorizationOptions(options));

        Assert.Null(exception);
        Assert.Null(options.GetPolicy("SecurityRoot"));
    }

}
