using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Constants;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Combines the remote Identity feature gate with the current caller's effective user grants.
/// </summary>
public sealed class UserAdministrationAccessService(
    IRemoteFeatureProvider remoteFeatureProvider,
    IIdentityPermissionContext permissionContext) : IUserAdministrationAccessService
{
    public Task<UserAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) =>
        IdentityAdministrationAccessResolver.ResolveAsync(
            remoteFeatureProvider,
            permissionContext,
            IdentityPermissions.UsersResource,
            UserAdministrationAccess.Unavailable,
            UserAdministrationAccess.Forbidden,
            (canCreate, canUpdate, canDelete) => new UserAdministrationAccess(
                UserAdministrationAccessState.Ready, CanView: true, canCreate, canUpdate, canDelete),
            cancellationToken);

    public void Invalidate() => permissionContext.Invalidate();
}
