using System.Security.Claims;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Authorizes role assignment and role permission changes.
/// </summary>
public interface IRoleAuthorizationService
{
    /// <summary>
    /// Returns true when the user can assign all requested roles.
    /// </summary>
    Task<bool> CanAssignRolesAsync(ClaimsPrincipal user, IEnumerable<string>? roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the user can create a role with the requested permissions.
    /// </summary>
    bool CanCreateRoleWithPermissions(ClaimsPrincipal user, IEnumerable<string>? permissions);

    /// <summary>
    /// Returns true when the user can mutate the specified role.
    /// </summary>
    /// <remarks>
    /// When replacement permissions are supplied, authorization is evaluated against both the current permissions and the replacement permissions so callers cannot modify permissions they do not already hold.
    /// </remarks>
    bool CanMutateRole(ClaimsPrincipal user, Role role, IEnumerable<string>? replacementPermissions = null);
}
