using System.Security.Claims;
using Elsa.ServerLogs.Permissions;

namespace Elsa.ServerLogs.UnitTests.Permissions;

public class ServerLogPermissionsTests
{
    [Theory]
    [InlineData(PermissionNames.All)]
    [InlineData(ServerLogPermissions.ReadAll)]
    [InlineData(ServerLogPermissions.Read)]
    public void CanRead_WhenUserHasAcceptedPermission_ReturnsTrue(string permission)
    {
        var user = CreateUser(permission);

        var canRead = ServerLogPermissions.CanRead(user);

        Assert.True(canRead);
    }

    [Fact]
    public void CanRead_WhenUserHasDifferentReadPermission_ReturnsFalse()
    {
        var user = CreateUser("read:workflow-definitions");

        var canRead = ServerLogPermissions.CanRead(user);

        Assert.False(canRead);
    }

    private static ClaimsPrincipal CreateUser(string permission)
    {
        var identity = new ClaimsIdentity([new("permissions", permission)], "test");
        return new(identity);
    }
}
