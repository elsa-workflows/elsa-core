using System.Security.Claims;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Permissions;

public class ExternalAuthenticationPermissionServiceTests
{
    [Fact]
    public async Task ListAsyncOmitsTheEmptySetSentinel()
    {
        var service = new ExternalAuthenticationPermissionService(new StaticAuthenticationStateProvider(
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("permissions", "none"),
                new Claim("permissions", "workflows:read")
            ], "test"))));

        var permissions = await service.ListAsync();

        Assert.DoesNotContain("none", permissions);
        Assert.Contains("workflows:read", permissions);
        Assert.True(await service.HasAsync("workflows:read"));
        Assert.False(await service.HasAsync("none"));
    }

    private sealed class StaticAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
