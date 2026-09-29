using Elsa.Studio.Authorization;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

public class PermissionTests
{
    [Theory]
    [InlineData("secrets:view", "secrets", "view")]
    [InlineData("  workflows/*:view  ", "workflows/*", "view")]
    [InlineData("*", "*", "*")]
    [InlineData("*:view", "*", "view")]
    public void TryParse_AcceptsWellFormedPermissions(string value, string resource, string verb)
    {
        Assert.True(Permission.TryParse(value, out var permission));
        Assert.Equal(new Permission(resource, verb), permission);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("secrets")]
    [InlineData(":view")]
    [InlineData("secrets:")]
    [InlineData("external-authentication:connections:read")]
    [InlineData("workflows:definitions/view")]
    [InlineData("secrets,labels:view")]
    public void TryParse_RejectsMalformedPermissions(string? value)
    {
        Assert.False(Permission.TryParse(value, out _));
    }

    [Fact]
    public void ToString_WritesResourceAndVerb() => Assert.Equal("workflows/instances:view", new Permission("workflows/instances", "view").ToString());
}
