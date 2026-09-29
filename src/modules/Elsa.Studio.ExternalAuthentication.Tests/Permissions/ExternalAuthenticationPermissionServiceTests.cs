using System.Security.Claims;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Permissions;

/// <summary>
/// Core 3.9 issues <c>{resource}:{verb}</c> grants with resource and verb wildcards. Studio used to compare the claims
/// verbatim against pre-3.9 names, so only users holding a bare <c>*</c> ever saw the External Authentication pages.
/// </summary>
public class ExternalAuthenticationPermissionServiceTests
{
    [Theory]
    [InlineData("external-authentication/connections:view")]
    [InlineData("external-authentication/connections:*")]
    [InlineData("external-authentication/*:view")]
    [InlineData("external-authentication/*:*")]
    [InlineData("*")]
    public async Task HasAsync_HonorsCoreGrantsAndWildcards(string grant)
    {
        Assert.True(await CreateService(grant).HasAsync(ExternalAuthenticationPermissions.Read));
    }

    [Theory]
    [InlineData("external-authentication/connections:update")]
    [InlineData("external-authentication/sessions:*")]
    [InlineData("external-authentication:connections:read")]
    public async Task HasAsync_RejectsGrantsThatDoNotCoverTheRequirement(string grant)
    {
        Assert.False(await CreateService(grant).HasAsync(ExternalAuthenticationPermissions.Read));
    }

    [Fact]
    public async Task HasAsync_FailsClosedWithoutPermissionClaims()
    {
        Assert.False(await CreateService().HasAsync(ExternalAuthenticationPermissions.Read));
    }

    private static ExternalAuthenticationPermissionService CreateService(params string[] grants) =>
        new(new StaticAuthenticationStateProvider(new(new ClaimsIdentity(grants.Select(x => new Claim("permissions", x)), "test"))));

    private sealed class StaticAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }
}
