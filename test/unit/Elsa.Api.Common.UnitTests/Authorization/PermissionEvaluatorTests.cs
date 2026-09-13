using System.Security.Claims;
using Elsa.Authorization;
using System.Threading.Tasks;

namespace Elsa.Api.Common.UnitTests.Authorization;

public class PermissionEvaluatorTests
{
    private readonly PermissionEvaluator _evaluator = new();

    private static ClaimsPrincipal PrincipalWith(params string[] permissions) =>
        new(new ClaimsIdentity(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)), "test"));

    [Test]
    public async Task GrantsAreTheUnionAcrossRoles()
    {
        // Claims arrive already flattened from every role the principal holds.
        var principal = PrincipalWith("workflows/definitions:view", "dashboard:view", "workflows/definitions:publish");

        var grants = _evaluator.GetGrants(principal);

        await Assert.That(grants.Count).IsEqualTo(3);
        await Assert.That(_evaluator.HasPermission(principal, "workflows/definitions", "publish")).IsTrue();
        await Assert.That(_evaluator.HasPermission(principal, "dashboard", "view")).IsTrue();
        await Assert.That(_evaluator.HasPermission(principal, "secrets", "view")).IsFalse();
    }

    [Test]
    public async Task NoVerbImpliesAnother()
    {
        // FR-009. Holding write does not confer view, in this model or the one it replaced.
        var principal = PrincipalWith("secrets:write");

        await Assert.That(_evaluator.HasPermission(principal, "secrets", "write")).IsTrue();
        await Assert.That(_evaluator.HasPermission(principal, "secrets", "view")).IsFalse();
        await Assert.That(_evaluator.HasPermission(principal, "secrets", "delete")).IsFalse();
    }

    [Test]
    public async Task ASeededWildcardStillAuthorizesEverything()
    {
        // The seeded admin role stores "*". It must keep working across the vocabulary migration.
        var principal = PrincipalWith("*");

        await Assert.That(_evaluator.HasPermission(principal, "workflows/definitions", "publish")).IsTrue();
        await Assert.That(_evaluator.HasPermission(principal, "identity/roles", "delete")).IsTrue();
    }

    [Test]
    public async Task MalformedClaimsAreSkippedRatherThanThrowing()
    {
        // One bad stored grant must not deny an entire principal.
        var principal = PrincipalWith("not-a-permission", "workflows/definitions:view", "");

        await Assert.That(_evaluator.HasPermission(principal, "workflows/definitions", "view")).IsTrue();
        await Assert.That(_evaluator.GetGrants(principal)).HasSingleItem();
    }

    [Test]
    public async Task ANullOrAnonymousPrincipalHoldsNothing()
    {
        await Assert.That(_evaluator.HasPermission(null, "dashboard", "view")).IsFalse();
        await Assert.That(_evaluator.GetGrants(null)).IsEmpty();
        await Assert.That(_evaluator.HasPermission(new ClaimsPrincipal(new ClaimsIdentity()), "dashboard", "view")).IsFalse();
    }

    [Test]
    public async Task HasAllPermissionsRequiresEveryOne()
    {
        var principal = PrincipalWith("workflows/*:view", "secrets:write");
        var required = new[] { "workflows/instances:view", "secrets:write" }.Select(Permission.Parse).ToArray();

        await Assert.That(_evaluator.HasAllPermissions(principal, required)).IsTrue();
        await Assert.That(_evaluator.HasAllPermissions(principal, [.. required, Permission.Parse("secrets:delete")])).IsFalse();
    }

    [Test]
    public async Task HasAllPermissionsIsVacuouslyTrueForNoRequirements()
    {
        await Assert.That(_evaluator.HasAllPermissions(PrincipalWith(), [])).IsTrue();
    }
}