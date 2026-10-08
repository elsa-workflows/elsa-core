using System.Security.Claims;
using Elsa;
using Elsa.AI.Host.Endpoints.AI;
using Microsoft.AspNetCore.Http;

namespace Elsa.AI.Host.UnitTests;

public class AIHttpContextIdentityTests
{
    [Fact]
    public void GetPermissionsOmitsTheEmptySetSentinel()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(PermissionNames.ClaimType, PermissionNames.None),
                new Claim(PermissionNames.ClaimType, "workflows:read")
            ], "test"))
        };

        var permissions = AIHttpContextIdentity.GetPermissions(context);

        Assert.DoesNotContain(PermissionNames.None, permissions);
        Assert.Equal(["workflows:read"], permissions);
    }

    [Fact]
    public void GetPermissionsIsEmptyWhenTheCallerHoldsOnlyTheSentinel()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(PermissionNames.ClaimType, PermissionNames.None)
            ], "test"))
        };

        Assert.Empty(AIHttpContextIdentity.GetPermissions(context));
    }
}
