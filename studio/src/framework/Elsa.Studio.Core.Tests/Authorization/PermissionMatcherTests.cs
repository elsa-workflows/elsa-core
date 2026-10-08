using Elsa.Studio.Authorization;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

public class PermissionMatcherTests
{
    [Theory]
    [InlineData("secrets:view", "secrets:view", true)]
    [InlineData("secrets:view", "secrets:write", false)]
    [InlineData("secrets:view", "workflows/definitions:view", false)]
    [InlineData("*", "workflows/instances:delete", true)]
    [InlineData("*:*", "secrets:write", true)]
    [InlineData("*:view", "dashboard:view", true)]
    [InlineData("*:view", "dashboard:write", false)]
    [InlineData("secrets:*", "secrets:test", true)]
    [InlineData("secrets:*", "labels:view", false)]
    [InlineData("workflows/*:view", "workflows:view", true)]
    [InlineData("workflows/*:view", "workflows/definitions:view", true)]
    [InlineData("workflows/*:view", "workflows/definitions/versions:view", true)]
    [InlineData("workflows/*:view", "workflows/definitions:write", false)]
    [InlineData("workflows/*:view", "workflows-legacy:view", false)]
    [InlineData("workflows/*:view", "workflowsx/definitions:view", false)]
    [InlineData("workflows/definitions:view", "workflows/definitions/versions:view", false)]
    [InlineData("workflows/definitions/*:*", "workflows/definitions:publish", true)]
    [InlineData("workflows/definitions/*:*", "workflows/instances:view", false)]
    [InlineData("workflows*:view", "workflows/definitions:view", false)]
    [InlineData("identity/users:view", "identity/users:view", true)]
    [InlineData("Secrets:view", "secrets:view", false)]
    public void Satisfies_FollowsTheBackendMatchingRules(string granted, string required, bool expected)
    {
        Assert.True(Permission.TryParse(granted, out var grantedPermission));
        Assert.True(Permission.TryParse(required, out var requiredPermission));

        Assert.Equal(expected, PermissionMatcher.Satisfies(grantedPermission, requiredPermission));
    }

    [Fact]
    public void Satisfies_WithSeveralGrants_PassesWhenAnyGrantMatches()
    {
        Permission[] grants = [new("secrets", "view"), new("labels", "*")];

        Assert.True(PermissionMatcher.Satisfies(grants, new("labels", "delete")));
        Assert.False(PermissionMatcher.Satisfies(grants, new("secrets", "delete")));
    }
}
