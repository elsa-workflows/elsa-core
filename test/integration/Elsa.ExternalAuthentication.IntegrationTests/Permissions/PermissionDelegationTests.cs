using System.Security.Claims;
using System.Text.Json;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Models;

namespace Elsa.ExternalAuthentication.IntegrationTests.Permissions;

public class PermissionDelegationTests
{
    [Fact]
    public async Task OrdinaryAdministratorMustPossessDelegationPermissionAndEveryGrantedPermission()
    {
        var authorizer = new DefaultPermissionDelegationAuthorizer(Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()));
        var selection = Selection("reports:view");

        var withoutGrantedPermission = await authorizer.AuthorizeAsync(
            Actor(ExternalAuthenticationPermissions.PermissionsDelegate),
            [selection]);
        var withGrantedPermission = await authorizer.AuthorizeAsync(
            Actor(ExternalAuthenticationPermissions.PermissionsDelegate, "reports:view"),
            [selection]);

        Assert.False(withoutGrantedPermission.IsAuthorized);
        Assert.Equal(["reports:view"], withoutGrantedPermission.UnauthorizedPermissions);
        Assert.True(withGrantedPermission.IsAuthorized);
    }

    [Fact]
    public async Task UnrestrictedDelegationCannotCrossDeploymentDenyBoundary()
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.DeniedPermissions = ["reports:view"];
        var authorizer = new DefaultPermissionDelegationAuthorizer(Microsoft.Extensions.Options.Options.Create(options));

        var result = await authorizer.AuthorizeAsync(
            Actor(ExternalAuthenticationPermissions.PermissionsDelegateUnrestricted, PermissionNames.All),
            [Selection("reports:view")]);

        Assert.False(result.IsAuthorized);
        Assert.Equal(["reports:view"], result.UnauthorizedPermissions);
    }

    private static GrantSourceSelection Selection(string permission) => new(
        "claim-mapping",
        1,
        JsonSerializer.SerializeToElement(new
        {
            claimType = "department",
            mappings = new Dictionary<string, string[]> { ["engineering"] = [permission] }
        }),
        0);

    private static ClaimsPrincipal Actor(params string[] permissions) => new(
        new ClaimsIdentity(permissions.Select(permission => new Claim(PermissionNames.ClaimType, permission)), "test"));
}
