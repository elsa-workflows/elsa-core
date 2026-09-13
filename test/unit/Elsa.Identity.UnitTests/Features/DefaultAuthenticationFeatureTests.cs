using Elsa.Features.Services;
using Elsa.Identity.Features;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using System.Threading.Tasks;

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

    [Test]
    public async Task DefaultAuthorizationConfigurationRegistersNoPolicy()
    {
        _feature.ConfigureAuthorizationOptions(_options);

        await Assert.That(_options.GetPolicy("SecurityRoot")).IsNull();
    }

    [Test]
    public async Task CustomAuthorizationConfigurationIsHonoured()
    {
        _feature.ConfigureAuthorizationOptions = options => options.AddPolicy("Custom", policy => policy.RequireAuthenticatedUser());

        _feature.ConfigureAuthorizationOptions(_options);

        await Assert.That(_options.GetPolicy("Custom")).IsNotNull();
        await Assert.That(_options.GetPolicy("SecurityRoot")).IsNull();
    }

    [Test]
    public async Task NullConfigureAuthorizationOptionsFallsBackToANoOp()
    {
        // A host clearing the hook must not take the process down on the next Apply().
        _feature.ConfigureAuthorizationOptions = null!;

        Exception? exception = null;
        try
        {
            _feature.ConfigureAuthorizationOptions(_options);
        }
        catch (Exception e)
        {
            exception = e;
        }

        await Assert.That(exception).IsNull();
        await Assert.That(_options.GetPolicy("SecurityRoot")).IsNull();
    }
}