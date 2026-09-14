using System.Security.Claims;
using Elsa.Authorization;

namespace Elsa.Api.Common.UnitTests.Authorization;

public class PermissionEvaluatorTests
{
    private readonly PermissionEvaluator _evaluator = new();

    private static ClaimsPrincipal PrincipalWith(params string[] permissions) =>
        new(new ClaimsIdentity(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)), "test"));

    [Fact]
    public void GrantsAreTheUnionAcrossRoles()
    {
        // Claims arrive already flattened from every role the principal holds.
        var principal = PrincipalWith("workflows/definitions:view", "dashboard:view", "workflows/definitions:publish");

        var grants = _evaluator.GetGrants(principal);

        Assert.Equal(3, grants.Count);
        Assert.True(_evaluator.HasPermission(principal, "workflows/definitions", "publish"));
        Assert.True(_evaluator.HasPermission(principal, "dashboard", "view"));
        Assert.False(_evaluator.HasPermission(principal, "secrets", "view"));
    }

    [Fact]
    public void NoVerbImpliesAnother()
    {
        // FR-009. Holding write does not confer view, in this model or the one it replaced.
        var principal = PrincipalWith("secrets:write");

        Assert.True(_evaluator.HasPermission(principal, "secrets", "write"));
        Assert.False(_evaluator.HasPermission(principal, "secrets", "view"));
        Assert.False(_evaluator.HasPermission(principal, "secrets", "delete"));
    }

    [Fact]
    public void ASeededWildcardStillAuthorizesEverything()
    {
        // The seeded admin role stores "*". It must keep working across the vocabulary migration.
        var principal = PrincipalWith("*");

        Assert.True(_evaluator.HasPermission(principal, "workflows/definitions", "publish"));
        Assert.True(_evaluator.HasPermission(principal, "identity/roles", "delete"));
    }

    [Fact]
    public void MalformedClaimsAreSkippedRatherThanThrowing()
    {
        // One bad stored grant must not deny an entire principal.
        var principal = PrincipalWith("not-a-permission", "workflows/definitions:view", "");

        Assert.True(_evaluator.HasPermission(principal, "workflows/definitions", "view"));
        Assert.Single(_evaluator.GetGrants(principal));
    }

    [Fact]
    public void ANullOrAnonymousPrincipalHoldsNothing()
    {
        Assert.False(_evaluator.HasPermission(null, "dashboard", "view"));
        Assert.Empty(_evaluator.GetGrants(null));
        Assert.False(_evaluator.HasPermission(new ClaimsPrincipal(new ClaimsIdentity()), "dashboard", "view"));
    }

    [Fact]
    public void HasAllPermissionsRequiresEveryOne()
    {
        var principal = PrincipalWith("workflows/*:view", "secrets:write");
        var required = new[] { "workflows/instances:view", "secrets:write" }.Select(Permission.Parse).ToArray();

        Assert.True(_evaluator.HasAllPermissions(principal, required));
        Assert.False(_evaluator.HasAllPermissions(principal, [.. required, Permission.Parse("secrets:delete")]));
    }

    [Fact]
    public void HasAllPermissionsIsVacuouslyTrueForNoRequirements()
    {
        Assert.True(_evaluator.HasAllPermissions(PrincipalWith(), []));
    }
}
