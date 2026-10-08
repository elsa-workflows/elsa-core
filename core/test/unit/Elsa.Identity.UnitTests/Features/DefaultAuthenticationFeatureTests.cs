using Elsa.Features.Services;
using Elsa.Identity.Features;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Features;

/// <summary>
/// These tests previously asserted the shape of the SecurityRoot policy. That policy has been retired in
/// favour of endpoint permissions (ADR 0010), so what matters now is that the feature registers no policy of
/// its own and still honours a host's own authorization configuration.
/// </summary>
public class DefaultAuthenticationFeatureTests
{
    private readonly DefaultAuthenticationFeature _feature = new(Substitute.For<IModule>());
    private readonly AuthorizationOptions _options = new();

    [Fact]
    public void DefaultAuthorizationConfigurationRegistersNoPolicy()
    {
        _feature.ConfigureAuthorizationOptions(_options);

        Assert.Null(_options.GetPolicy("SecurityRoot"));
    }

    [Fact]
    public void CustomAuthorizationConfigurationIsHonoured()
    {
        _feature.ConfigureAuthorizationOptions = options => options.AddPolicy("Custom", policy => policy.RequireAuthenticatedUser());

        _feature.ConfigureAuthorizationOptions(_options);

        Assert.NotNull(_options.GetPolicy("Custom"));
        Assert.Null(_options.GetPolicy("SecurityRoot"));
    }

    [Fact]
    public void NullConfigureAuthorizationOptionsFallsBackToANoOp()
    {
        // A host clearing the hook must not take the process down on the next Apply().
        _feature.ConfigureAuthorizationOptions = null!;

        var exception = Record.Exception(() => _feature.ConfigureAuthorizationOptions(_options));

        Assert.Null(exception);
        Assert.Null(_options.GetPolicy("SecurityRoot"));
    }
}
