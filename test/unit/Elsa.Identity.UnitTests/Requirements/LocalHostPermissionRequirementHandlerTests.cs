using System.Net;
using System.Security.Claims;
using Elsa.Options;
using Elsa.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Identity.UnitTests.Requirements;

public class LocalHostPermissionRequirementHandlerTests
{
    [Fact]
    public async Task DoesNotGrantPermissionsToLocalhostRequestsByDefault()
    {
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: false, isLocal: true);

        Assert.False(context.HasSucceeded);
        Assert.Empty(context.User.FindAll("permissions"));
    }

    [Fact]
    public async Task GrantsPermissionsToLocalhostRequestsWhenExplicitlyEnabled()
    {
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: true, isLocal: true);
        var permissions = context.User.FindAll("permissions").Select(x => x.Value).ToList();

        Assert.True(context.HasSucceeded);
        Assert.Contains("create:application", permissions);
        Assert.Contains("create:user", permissions);
        Assert.Contains("create:role", permissions);
    }

    [Fact]
    public async Task DoesNotGrantPermissionsToRemoteRequestsWhenExplicitlyEnabled()
    {
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: true, isLocal: false);

        Assert.False(context.HasSucceeded);
        Assert.Empty(context.User.FindAll("permissions"));
    }

    private static async Task<AuthorizationHandlerContext> AuthorizeAsync(bool enableLocalHostPermissionGrant, bool isLocal)
    {
        var requirement = new LocalHostPermissionRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        var httpContext = CreateHttpContext(isLocal);
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var options = OptionsFactory.Create(new LocalHostPermissionRequirementOptions
        {
            EnableLocalHostPermissionGrant = enableLocalHostPermissionGrant
        });
        var handler = new LocalHostPermissionRequirementHandler(httpContextAccessor, options);

        await handler.HandleAsync(authorizationContext);

        return authorizationContext;
    }

    private static HttpContext CreateHttpContext(bool isLocal)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.LocalIpAddress = IPAddress.Loopback;
        httpContext.Connection.RemoteIpAddress = isLocal ? IPAddress.Loopback : IPAddress.Parse("10.0.0.1");
        return httpContext;
    }
}
