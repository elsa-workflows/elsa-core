using Elsa.Studio.Authorization;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Adapts <see cref="IIdentityPermissionContext"/> (<c>GET /identity/me/permissions</c>) to
/// <see cref="IPermissionService"/> so Studio's shell can gate navigation, pages and actions.
/// </summary>
/// <remarks>
/// Fail closed: a forbidden or unavailable snapshot becomes a known empty grant set, not
/// <see cref="UserPermissions.Unknown"/>. That keeps third-party / missing-claim tokens from
/// fail-opening the way 3.9's <c>ClaimsPermissionService</c> did.
/// </remarks>
public sealed class IdentityPermissionService(IIdentityPermissionContext context) : IPermissionService
{
    /// <inheritdoc />
    public async ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await context.GetAsync(cancellationToken);

        if (snapshot.State != IdentityPermissionSnapshotState.Ready)
        {
            return UserPermissions.FromGrants([]);
        }

        var grants = snapshot.Grants.SelectMany(pair => pair.Value.Select(verb => new Permission(pair.Key, verb)));
        return UserPermissions.FromGrants(grants);
    }
}
