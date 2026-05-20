using Elsa.Features.Services;
using Elsa.Identity.Features;
using Elsa.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Features;

public class DefaultAuthenticationFeatureTests
{
    [Fact]
    public void DefaultSecurityRootPolicyRequiresAuthenticatedUser()
    {
        var feature = new DefaultAuthenticationFeature(Substitute.For<IModule>());
        var options = new AuthorizationOptions();

        feature.ConfigureAuthorizationOptions(options);

        var policy = options.GetPolicy(IdentityPolicyNames.SecurityRoot);

        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
        Assert.DoesNotContain(policy.Requirements, requirement => requirement is LocalHostPermissionRequirement);
    }

    [Fact]
    public void EnableLocalHostPermissionGrantForSecurityRootConfiguresExplicitLocalhostPolicy()
    {
        var feature = new DefaultAuthenticationFeature(Substitute.For<IModule>());
        var options = new AuthorizationOptions();

        feature.EnableLocalHostPermissionGrantForSecurityRoot();
        feature.ConfigureAuthorizationOptions(options);

        var policy = options.GetPolicy(IdentityPolicyNames.SecurityRoot);

        Assert.True(feature.EnableLocalHostPermissionGrant);
        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement => requirement is LocalHostPermissionRequirement);
    }

    [Fact]
    public void EnableLocalHostPermissionGrantDoesNotOverwriteCustomAuthorizationConfiguration()
    {
        var feature = new DefaultAuthenticationFeature(Substitute.For<IModule>());
        feature.ConfigureAuthorizationOptions = options => options.AddPolicy("Custom", policy => policy.RequireAuthenticatedUser());

        feature.EnableLocalHostPermissionGrantForSecurityRoot();
        var options = new AuthorizationOptions();
        feature.ConfigureAuthorizationOptions(options);

        Assert.True(feature.EnableLocalHostPermissionGrant);
        Assert.NotNull(options.GetPolicy("Custom"));
        Assert.Null(options.GetPolicy(IdentityPolicyNames.SecurityRoot));
    }

    [Fact]
    public void NullConfigureAuthorizationOptionsFallsBackToDefaultSecurityRootPolicy()
    {
        var feature = new DefaultAuthenticationFeature(Substitute.For<IModule>())
        {
            ConfigureAuthorizationOptions = null!
        };
        var options = new AuthorizationOptions();

        feature.ConfigureAuthorizationOptions(options);

        var policy = options.GetPolicy(IdentityPolicyNames.SecurityRoot);

        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void DisableLocalHostPermissionGrantForSecurityRootClearsOptInFlag()
    {
        var feature = new DefaultAuthenticationFeature(Substitute.For<IModule>());

        feature.EnableLocalHostPermissionGrantForSecurityRoot();
        feature.DisableLocalHostPermissionGrantForSecurityRoot();

        Assert.False(feature.EnableLocalHostPermissionGrant);
    }
}
