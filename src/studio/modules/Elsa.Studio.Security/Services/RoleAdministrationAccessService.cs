using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Constants;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Combines the remote Identity feature gate with the current caller's effective role grants.
/// </summary>
public sealed class RoleAdministrationAccessService(
    IRemoteFeatureProvider remoteFeatureProvider,
    IIdentityPermissionContext permissionContext) : IRoleAdministrationAccessService
{
    public Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) =>
        IdentityAdministrationAccessResolver.ResolveAsync(
            remoteFeatureProvider,
            permissionContext,
            IdentityPermissions.RolesResource,
            RoleAdministrationAccess.Unavailable,
            RoleAdministrationAccess.Forbidden,
            (canCreate, canUpdate, canDelete) => new RoleAdministrationAccess(
                RoleAdministrationAccessState.Ready, CanView: true, canCreate, canUpdate, canDelete),
            cancellationToken);

    public void Invalidate() => permissionContext.Invalidate();
}
