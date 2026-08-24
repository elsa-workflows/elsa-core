using System.Security.Claims;
using Elsa.Authorization;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public class RoleAuthorizationService(IRoleProvider roleProvider, IPermissionEvaluator evaluator) : IRoleAuthorizationService
{
    /// <inheritdoc />
    public async Task<bool> CanAssignRolesAsync(ClaimsPrincipal user, IEnumerable<string>? roleIds, CancellationToken cancellationToken = default)
    {
        var requestedRoleIds = roleIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (requestedRoleIds == null || requestedRoleIds.Count == 0)
            return true;

        var requestedRoleIdSet = requestedRoleIds.ToHashSet(StringComparer.Ordinal);
        var roles = (await roleProvider.FindByIdsAsync(requestedRoleIds, cancellationToken))
            .Where(x => requestedRoleIdSet.Contains(x.Id))
            .ToList();
        var resolvedRoleIdSet = roles.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        if (!resolvedRoleIdSet.SetEquals(requestedRoleIdSet))
            return false;

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

    /// <remarks>
    /// Containment is evaluated through <see cref="IPermissionEvaluator"/> rather than by set membership,
    /// so a caller holding <c>workflows/*:view</c> may grant <c>workflows/definitions:view</c>, while a
    /// caller holding only the concrete grant may not grant the broader wildcard. Set membership would get
    /// the first case wrong and force administrators to hold every concrete grant they wish to delegate.
    /// </remarks>
    private bool HasAllPermissions(ClaimsPrincipal user, IEnumerable<string>? permissions)
    {
        if (permissions is null)
            return true;

        var required = permissions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Select(x => Permission.TryParse(x, out var permission) ? permission : (Permission?)null)
            .ToArray();

        // A permission that does not parse cannot be reasoned about, so it is never delegable.
        if (required.Any(x => x is null))
            return false;

        return evaluator.HasAllPermissions(user, required.Select(x => x!.Value));
    }
}
