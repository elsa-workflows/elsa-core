using Elsa.Studio.Authorization;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Permissions;

public sealed class ExternalAuthenticationPermissionServiceTests
{
    [Theory]
    [InlineData("external-authentication/connections:view")]
    [InlineData("external-authentication/connections:*")]
    [InlineData("external-authentication/*:view")]
    [InlineData("external-authentication/*:*")]
    [InlineData("*")]
    public async Task HasAsync_HonorsCoreGrantsAndWildcards(string grant)
    {
        Assert.True(Permission.TryParse(grant, out var parsed));
        var inner = new FixedPermissionService(UserPermissions.FromGrants([parsed]));
        var service = new ExternalAuthenticationPermissionService(inner);

        Assert.True(await service.HasAsync(ExternalAuthenticationPermissions.Read));
    }

    [Theory]
    [InlineData("external-authentication/connections:update")]
    [InlineData("external-authentication/sessions:*")]
    [InlineData("external-authentication:connections:read")]
    public async Task HasAsync_RejectsGrantsThatDoNotCoverTheRequirement(string grant)
    {
        var inner = Permission.TryParse(grant, out var parsed)
            ? new FixedPermissionService(UserPermissions.FromGrants([parsed]))
            : new FixedPermissionService(UserPermissions.FromGrants([]));
        var service = new ExternalAuthenticationPermissionService(inner);

        Assert.False(await service.HasAsync(ExternalAuthenticationPermissions.Read));
    }

    [Fact]
    public async Task HasAsync_UsesTheSharedPermissionServiceAndFailsClosed()
    {
        var inner = new FixedPermissionService(UserPermissions.FromGrants([new Permission("external-authentication/connections", "view")]));
        var service = new ExternalAuthenticationPermissionService(inner);

        Assert.True(await service.HasAsync(ExternalAuthenticationPermissions.Read));
        Assert.False(await service.HasAsync(ExternalAuthenticationPermissions.Create));
        Assert.False(await service.HasAsync("external-authentication:connections:read"));
    }

    [Fact]
    public async Task HasAsync_WhenPermissionServiceIsMissing_FailsClosed()
    {
        var service = new ExternalAuthenticationPermissionService();

        Assert.False(await service.HasAsync(ExternalAuthenticationPermissions.Read));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task HasAsync_WhenGrantsAreUnknown_RejectsLegacyNames()
    {
        var service = new ExternalAuthenticationPermissionService(new FixedPermissionService(UserPermissions.Unknown));

        Assert.False(await service.HasAsync("external-authentication:connections:read"));
    }

    [Fact]
    public async Task ListAsync_OmitsTheEmptySetSentinel()
    {
        var service = new ExternalAuthenticationPermissionService(new FixedPermissionService(UserPermissions.FromGrants(
        [
            new Permission("none", "view"),
            new Permission("external-authentication/connections", "view")
        ])));

        var permissions = await service.ListAsync();

        Assert.DoesNotContain("none", permissions);
        Assert.DoesNotContain("none:view", permissions);
        Assert.Contains(ExternalAuthenticationPermissions.Read, permissions);
        Assert.False(await service.HasAsync("none"));
        Assert.True(await service.HasAsync(ExternalAuthenticationPermissions.Read));
    }

    private sealed class FixedPermissionService(UserPermissions permissions) : IPermissionService
    {
        public ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken = default) => new(permissions);
    }
}
