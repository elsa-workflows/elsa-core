using System.Net;
using System.Security.Claims;
using Elsa;
using Elsa.Authorization;
using Elsa.Options;
using Elsa.Requirements;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        Assert.Empty(context.User.FindAll(PermissionNames.ClaimType));
    }

    [Fact]
    public async Task GrantsPermissionsToLocalhostRequestsWhenExplicitlyEnabled()
    {
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: true, isLocal: true);
        var permissions = context.User.FindAll(PermissionNames.ClaimType).Select(x => x.Value).ToList();

        Assert.True(context.HasSucceeded);
        Assert.Contains("identity/applications:create", permissions);
        Assert.Contains("identity/users:create", permissions);
        Assert.Contains("identity/roles:create", permissions);
    }

    [Theory]
    [InlineData("identity/applications", "create")]
    [InlineData("identity/users", "create")]
    [InlineData("identity/roles", "create")]
    public async Task GrantedBootstrapPermissionsActuallyAuthorizeTheirEndpoints(string resource, string verb)
    {
        // The point of the grant, asserted through the evaluator rather than by string equality. The list
        // previously held legacy spellings ("create:user" and friends), which parse to a different pair
        // entirely, so the bootstrap grant authorized none of the three endpoints it exists to unlock.
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: true, isLocal: true);

        Assert.True(PermissionEvaluator.Shared.HasPermission(context.User, resource, verb));
    }

    [Fact]
    public async Task SucceedsForAnAuthenticatedCallerHoldingAWildcardThatCoversTheBootstrapGrants()
    {
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: true, isLocal: true, isAuthenticated: true, permissions: ["identity/*:create"]);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task DoesNotGrantPermissionsToAuthenticatedLocalhostRequestsWhenExplicitlyEnabled()
    {
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: true, isLocal: true, isAuthenticated: true);

        Assert.False(context.HasSucceeded);
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Empty(context.User.FindAll(PermissionNames.ClaimType));
    }

    [Fact]
    public async Task SucceedsForAuthenticatedLocalhostRequestsWithExistingBootstrapPermissionsWhenExplicitlyEnabled()
    {
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: true, isLocal: true, isAuthenticated: true, permissions: BootstrapPermissions);
        var permissions = context.User.FindAll(PermissionNames.ClaimType).Select(x => x.Value).ToList();

        Assert.True(context.HasSucceeded);
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal(3, permissions.Count);
        Assert.Contains("identity/applications:create", permissions);
        Assert.Contains("identity/users:create", permissions);
        Assert.Contains("identity/roles:create", permissions);
    }

    [Fact]
    public async Task DoesNotDuplicateBootstrapPermissionsAcrossMultipleEvaluations()
    {
        var requirement = new LocalHostPermissionRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        var handler = CreateHandler(enableLocalHostPermissionGrant: true, isLocal: true);

        await handler.HandleAsync(authorizationContext);
        await handler.HandleAsync(authorizationContext);

        Assert.True(authorizationContext.HasSucceeded);
        Assert.Equal(1, authorizationContext.User.Identities.Count(x => x.AuthenticationType == JwtBearerDefaults.AuthenticationScheme));
        Assert.Equal(3, authorizationContext.User.FindAll(PermissionNames.ClaimType).Count());
    }

    [Fact]
    public async Task DoesNotGrantPermissionsToRemoteRequestsWhenExplicitlyEnabled()
    {
        var context = await AuthorizeAsync(enableLocalHostPermissionGrant: true, isLocal: false);

        Assert.False(context.HasSucceeded);
        Assert.Empty(context.User.FindAll(PermissionNames.ClaimType));
    }

    private static readonly string[] BootstrapPermissions =
    [
        "identity/applications:create",
        "identity/users:create",
        "identity/roles:create"
    ];

    private static async Task<AuthorizationHandlerContext> AuthorizeAsync(bool enableLocalHostPermissionGrant, bool isLocal, bool isAuthenticated = false, params string[] permissions)
    {
        var requirement = new LocalHostPermissionRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity(isAuthenticated ? "Test" : null));
        user.Identities.First().AddClaims(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)));
        var authorizationContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        var handler = CreateHandler(enableLocalHostPermissionGrant, isLocal);

        await handler.HandleAsync(authorizationContext);

        return authorizationContext;
    }

    private static LocalHostPermissionRequirementHandler CreateHandler(bool enableLocalHostPermissionGrant, bool isLocal)
    {
        var httpContext = CreateHttpContext(isLocal);
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var options = OptionsFactory.Create(new LocalHostPermissionRequirementOptions
        {
            EnableLocalHostPermissionGrant = enableLocalHostPermissionGrant
        });

        return new LocalHostPermissionRequirementHandler(httpContextAccessor, options);
    }

    private static HttpContext CreateHttpContext(bool isLocal)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.LocalIpAddress = IPAddress.Loopback;
        httpContext.Connection.RemoteIpAddress = isLocal ? IPAddress.Loopback : IPAddress.Parse("10.0.0.1");
        return httpContext;
    }
}
