using System.Security.Claims;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class UserManagementAuthorizationExtensions
{
    /// <summary>
    /// Returns whether <paramref name="caller"/> may change or delete <paramref name="target"/>: the caller must hold every
    /// permission the target's tokens would carry.
    /// </summary>
    /// <remarks>
    /// The target's permissions are resolved exactly as the token issuer resolves them: the permissions of every role the
    /// role provider returns for the target's role IDs. That keeps the check aligned with case-insensitive stores and with
    /// providers that return roles regardless of the requested IDs. Role IDs of deleted roles resolve to nothing and grant
    /// nothing, so they don't block management of the user.
    /// </remarks>
    public static async Task<bool> CanManageUserAsync(
        this IRoleAuthorizationService roleAuthorizationService,
        IRoleProvider roleProvider,
        ClaimsPrincipal caller,
        User target,
        CancellationToken cancellationToken)
    {
        var roles = await roleProvider.FindByIdsAsync(target.Roles, cancellationToken);
        var permissions = roles.SelectMany(x => x.Permissions).ToList();
        return roleAuthorizationService.CanCreateRoleWithPermissions(caller, permissions);
    }
}
