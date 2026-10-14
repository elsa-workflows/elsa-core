using System.Security.Claims;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class UserManagementAuthorizationExtensions
{
    /// <summary>
    /// Returns whether <paramref name="caller"/> may change or delete <paramref name="target"/>: the caller must be able to
    /// assign every role the target currently holds.
    /// </summary>
    /// <remarks>
    /// Deleting a role does not remove its ID from users, so a user can carry IDs of roles that no longer exist.
    /// Those IDs grant nothing (tokens and permission stamps only resolve existing roles), so they are ignored here.
    /// Otherwise <see cref="IRoleAuthorizationService.CanAssignRolesAsync"/> would fail closed on them and nobody,
    /// not even a full administrator, could manage that user. If a role with the same ID is created again, it resolves
    /// and is checked again.
    /// </remarks>
    public static async Task<bool> CanManageUserAsync(
        this IRoleAuthorizationService roleAuthorizationService,
        IRoleProvider roleProvider,
        ClaimsPrincipal caller,
        User target,
        CancellationToken cancellationToken)
    {
        var existingRoleIds = (await roleProvider.FindByIdsAsync(target.Roles, cancellationToken)).Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var currentRoleIds = target.Roles.Where(existingRoleIds.Contains).ToList();
        return await roleAuthorizationService.CanAssignRolesAsync(caller, currentRoleIds, cancellationToken);
    }
}
