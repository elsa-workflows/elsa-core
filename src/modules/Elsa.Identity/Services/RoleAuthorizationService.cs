using System.Security.Claims;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public class RoleAuthorizationService(IRoleProvider roleProvider) : IRoleAuthorizationService
{
    private const string PermissionClaimType = "permissions";

    /// <inheritdoc />
    public async Task<bool> CanAssignRolesAsync(ClaimsPrincipal user, IEnumerable<string>? roleIds, CancellationToken cancellationToken = default)
    {
        var requestedRoleIds = roleIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (requestedRoleIds == null || requestedRoleIds.Count == 0)
            return true;

        var requestedRoleIdSet = requestedRoleIds.ToHashSet(StringComparer.Ordinal);
        var roles = (await roleProvider.FindByIdsAsync(requestedRoleIds, cancellationToken)).Where(x => requestedRoleIdSet.Contains(x.Id));
        var permissions = roles.SelectMany(x => x.Permissions);
        return HasAllPermissions(user, permissions);
    }

    /// <inheritdoc />
    public bool CanCreateRoleWithPermissions(ClaimsPrincipal user, IEnumerable<string>? permissions) => HasAllPermissions(user, permissions);

    /// <inheritdoc />
    public bool CanMutateRole(ClaimsPrincipal user, Role role, IEnumerable<string>? replacementPermissions = null)
    {
        var permissions = replacementPermissions == null
            ? role.Permissions
            : role.Permissions.Concat(replacementPermissions);

        return HasAllPermissions(user, permissions);
    }

    private static bool HasAllPermissions(ClaimsPrincipal user, IEnumerable<string>? permissions)
    {
        var grantedPermissions = user
            .FindAll(PermissionClaimType)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        if (grantedPermissions.Contains(PermissionNames.All))
            return true;

        return permissions?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .All(grantedPermissions.Contains) ?? true;
    }
}
