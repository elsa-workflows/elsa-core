using Elsa.Authorization;

namespace Elsa.Api.Common.UnitTests.Authorization;

public class PermissionMatcherTests
{
    [Theory]
    // Exact match on both axes.
    [InlineData("workflows/definitions:view", "workflows/definitions:view", true)]
    [InlineData("workflows/definitions:view", "workflows/definitions:delete", false)]
    [InlineData("workflows/definitions:view", "workflows/instances:view", false)]
    // A subtree wildcard covers the named node itself as well as its descendants.
    [InlineData("workflows/*:view", "workflows/definitions:view", true)]
    [InlineData("workflows/*:view", "workflows/definitions/versions:view", true)]
    [InlineData("workflows/definitions/*:view", "workflows/definitions:view", true)]
    [InlineData("workflows/definitions/*:view", "workflows/definitions/labels:view", true)]
    // ... but not a sibling that merely shares a prefix string.
    [InlineData("workflows/definition/*:view", "workflows/definitions:view", false)]
    [InlineData("workflows/*:view", "identity/users:view", false)]
    // A subtree wildcard does not widen the verb axis.
    [InlineData("workflows/*:view", "workflows/definitions:delete", false)]
    // A verb wildcard covers any verb on the matched resource.
    [InlineData("workflows/definitions:*", "workflows/definitions:delete", true)]
    [InlineData("workflows/definitions:*", "workflows/instances:delete", false)]
    // The whole vocabulary.
    [InlineData("*:*", "workflows/definitions:publish", true)]
    [InlineData("*:view", "anything/at/all:view", true)]
    [InlineData("*:view", "anything/at/all:delete", false)]
    // A concrete grant never widens.
    [InlineData("workflows/definitions:view", "workflows/*:view", false)]
    public void MatchesAsDeclared(string granted, string required, bool expected)
    {
        Assert.Equal(expected, PermissionMatcher.Satisfies(Permission.Parse(granted), Permission.Parse(required)));
    }

    [Fact]
    public void AWildcardCoversAResourceRegisteredLater()
    {
        // Forward reach is the whole point of a wildcard: a module added next release is covered without
        // touching the role.
        var granted = Permission.Parse("workflows/*:view");

        Assert.True(PermissionMatcher.Satisfies(granted, Permission.Parse("workflows/not-invented-yet:view")));
    }

    [Fact]
    public void AVerbWildcardCoversAVerbAddedLater()
    {
        var granted = Permission.Parse("secrets:*");

        Assert.True(PermissionMatcher.Satisfies(granted, Permission.Parse("secrets:quarantine")));
    }

    [Fact]
    public void ConcreteGrantsDoNotCoverVerbsAddedLater()
    {
        // The counterpart to the above: explicit grants stay frozen, which is what makes them safe.
        var granted = new[] { "secrets:view", "secrets:write", "secrets:delete" }.Select(Permission.Parse);

        Assert.False(PermissionMatcher.Satisfies(granted, Permission.Parse("secrets:quarantine")));
    }

    [Fact]
    public void AnEmptyGrantSetDeniesEverything()
    {
        Assert.False(PermissionMatcher.Satisfies([], Permission.Parse("dashboard:view")));
    }

    [Fact]
    public void AnyMatchingGrantSatisfiesTheRequirement()
    {
        var granted = new[] { "dashboard:view", "workflows/*:view" }.Select(Permission.Parse).ToArray();

        Assert.True(PermissionMatcher.Satisfies(granted, Permission.Parse("workflows/instances:view")));
        Assert.False(PermissionMatcher.Satisfies(granted, Permission.Parse("secrets:view")));
    }
}
