using Elsa.Authorization;
using System.Threading.Tasks;

namespace Elsa.Api.Common.UnitTests.Authorization;

public class PermissionMatcherTests
{
    [Test]
    // Exact match on both axes.
    [Arguments("workflows/definitions:view", "workflows/definitions:view", true)]
    [Arguments("workflows/definitions:view", "workflows/definitions:delete", false)]
    [Arguments("workflows/definitions:view", "workflows/instances:view", false)]
    // A subtree wildcard covers the named node itself as well as its descendants.
    [Arguments("workflows/*:view", "workflows/definitions:view", true)]
    [Arguments("workflows/*:view", "workflows/definitions/versions:view", true)]
    [Arguments("workflows/definitions/*:view", "workflows/definitions:view", true)]
    [Arguments("workflows/definitions/*:view", "workflows/definitions/labels:view", true)]
    // ... but not a sibling that merely shares a prefix string.
    [Arguments("workflows/definition/*:view", "workflows/definitions:view", false)]
    [Arguments("workflows/*:view", "identity/users:view", false)]
    // A subtree wildcard does not widen the verb axis.
    [Arguments("workflows/*:view", "workflows/definitions:delete", false)]
    // A verb wildcard covers any verb on the matched resource.
    [Arguments("workflows/definitions:*", "workflows/definitions:delete", true)]
    [Arguments("workflows/definitions:*", "workflows/instances:delete", false)]
    // The whole vocabulary.
    [Arguments("*:*", "workflows/definitions:publish", true)]
    [Arguments("*:view", "anything/at/all:view", true)]
    [Arguments("*:view", "anything/at/all:delete", false)]
    // A concrete grant never widens.
    [Arguments("workflows/definitions:view", "workflows/*:view", false)]
    public async Task MatchesAsDeclared(string granted, string required, bool expected)
    {
        await Assert.That(PermissionMatcher.Satisfies(Permission.Parse(granted), Permission.Parse(required))).IsEqualTo(expected);
    }

    [Test]
    public async Task AWildcardCoversAResourceRegisteredLater()
    {
        // Forward reach is the whole point of a wildcard: a module added next release is covered without
        // touching the role.
        var granted = Permission.Parse("workflows/*:view");

        await Assert.That(PermissionMatcher.Satisfies(granted, Permission.Parse("workflows/not-invented-yet:view"))).IsTrue();
    }

    [Test]
    public async Task AVerbWildcardCoversAVerbAddedLater()
    {
        var granted = Permission.Parse("secrets:*");

        await Assert.That(PermissionMatcher.Satisfies(granted, Permission.Parse("secrets:quarantine"))).IsTrue();
    }

    [Test]
    public async Task ConcreteGrantsDoNotCoverVerbsAddedLater()
    {
        // The counterpart to the above: explicit grants stay frozen, which is what makes them safe.
        var granted = new[] { "secrets:view", "secrets:write", "secrets:delete" }.Select(Permission.Parse);

        await Assert.That(PermissionMatcher.Satisfies(granted, Permission.Parse("secrets:quarantine"))).IsFalse();
    }

    [Test]
    public async Task AnEmptyGrantSetDeniesEverything()
    {
        await Assert.That(PermissionMatcher.Satisfies([], Permission.Parse("dashboard:view"))).IsFalse();
    }

    [Test]
    public async Task AnyMatchingGrantSatisfiesTheRequirement()
    {
        var granted = new[] { "dashboard:view", "workflows/*:view" }.Select(Permission.Parse).ToArray();

        await Assert.That(PermissionMatcher.Satisfies(granted, Permission.Parse("workflows/instances:view"))).IsTrue();
        await Assert.That(PermissionMatcher.Satisfies(granted, Permission.Parse("secrets:view"))).IsFalse();
    }
}