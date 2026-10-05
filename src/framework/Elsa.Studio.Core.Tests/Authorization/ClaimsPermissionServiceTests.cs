using System.Security.Claims;
using Elsa.Studio.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

public class ClaimsPermissionServiceTests
{
    private static readonly Permission SecretsView = new("secrets", "view");
    private static readonly Permission WorkflowInstancesView = new("workflows/instances", "view");

    [Fact]
    public async Task WithoutAnAuthenticationStateProvider_PermissionsAreUnknownAndEveryCheckPasses()
    {
        var permissions = await new ClaimsPermissionService().GetPermissionsAsync();

        Assert.False(permissions.IsKnown);
        Assert.True(permissions.Has(WorkflowInstancesView));
    }

    [Fact]
    public async Task ForAnAnonymousUser_PermissionsAreUnknown()
    {
        var permissions = await GetPermissionsAsync(new ClaimsIdentity());

        Assert.False(permissions.IsKnown);
    }

    [Fact]
    public async Task WhenTheTokenCarriesNoPermissionClaims_PermissionsAreUnknownAndEveryCheckPasses()
    {
        var permissions = await GetPermissionsAsync(Authenticated(new Claim("sub", "alice")));

        Assert.False(permissions.IsKnown);
        Assert.True(permissions.Has(WorkflowInstancesView));
    }

    [Fact]
    public async Task WithPermissionClaims_OnlyGrantedPermissionsPass()
    {
        var permissions = await GetPermissionsAsync(Authenticated(Grant("secrets:view"), Grant("secrets:write")));

        Assert.True(permissions.IsKnown);
        Assert.True(permissions.Has(SecretsView));
        Assert.False(permissions.Has(WorkflowInstancesView));
        Assert.Equal([WorkflowInstancesView], permissions.GetMissing([SecretsView, WorkflowInstancesView]));
    }

    [Fact]
    public async Task TheWildcardGrant_PassesEveryCheck()
    {
        var permissions = await GetPermissionsAsync(Authenticated(Grant("*")));

        Assert.True(permissions.HasAll([SecretsView, WorkflowInstancesView, new("identity/roles", "delete")]));
    }

    [Fact]
    public async Task MalformedGrants_AreSkippedWithoutDenyingTheRest()
    {
        var permissions = await GetPermissionsAsync(Authenticated(Grant("read:workflow-definitions:legacy"), Grant("workflows/*:view")));

        Assert.True(permissions.IsKnown);
        Assert.True(permissions.Has(WorkflowInstancesView));
        Assert.False(permissions.Has(SecretsView));
    }

    [Fact]
    public async Task OnlyMalformedGrants_AreKnownAndDenyEverything()
    {
        var permissions = await GetPermissionsAsync(Authenticated(Grant("external-authentication:connections:read")));

        Assert.True(permissions.IsKnown);
        Assert.False(permissions.Has(SecretsView));
    }

    private static Task<UserPermissions> GetPermissionsAsync(ClaimsIdentity identity) =>
        new ClaimsPermissionService(new StaticAuthenticationStateProvider(identity)).GetPermissionsAsync().AsTask();

    private static ClaimsIdentity Authenticated(params Claim[] claims) => new(claims, "test");

    private static Claim Grant(string permission) => new(ClaimsPermissionService.ClaimType, permission);

    private sealed class StaticAuthenticationStateProvider(ClaimsIdentity identity) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new(identity)));
    }
}
