using Elsa.Studio.Authorization;
using Elsa.Studio.Testing;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

public class UserPermissionsTests
{
    private static readonly Permission[] DashboardOrInstances = [new("dashboard", PermissionVerbs.View), new("workflows/instances", PermissionVerbs.View)];

    [Theory]
    [InlineData("workflows/instances:view", true)]
    [InlineData("workflows/*:view", true)]
    [InlineData("dashboard:view", true)]
    [InlineData("secrets:view", false)]
    [InlineData(null, true)]
    public void HasAny_PassesWhenOneOfTheRequiredPermissionsIsHeld(string? grant, bool expected)
    {
        var permissions = grant == null ? UserPermissions.Unknown : StubPermissionService.Grants(grant);

        Assert.Equal(expected, permissions.HasAny(DashboardOrInstances));
    }

    [Theory]
    [InlineData(new[] { "secrets:view", "labels:view" }, new[] { "labels:view", "secrets:view" }, true)]
    [InlineData(new[] { "secrets:view" }, new[] { "secrets:view", "labels:view" }, false)]
    [InlineData(new[] { "secrets:view" }, new[] { "labels:view" }, false)]
    public void IsEquivalentTo_ComparesTheGrantsRegardlessOfOrder(string[] grants, string[] otherGrants, bool expected) =>
        Assert.Equal(expected, StubPermissionService.Grants(grants).IsEquivalentTo(StubPermissionService.Grants(otherGrants)));

    [Fact]
    public void IsEquivalentTo_TellsUnknownApartFromKnownWithoutGrants()
    {
        Assert.True(UserPermissions.Unknown.IsEquivalentTo(UserPermissions.Unknown));
        Assert.False(UserPermissions.Unknown.IsEquivalentTo(StubPermissionService.Grants()));
        Assert.False(StubPermissionService.Grants().IsEquivalentTo(null));
    }
}
