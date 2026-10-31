using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Security.Constants;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Shared fail-closed resolution for one Identity administration resource: the remote Identity feature must be
/// enabled and the caller must hold the <c>view</c> verb before any capability is exposed.
/// </summary>
internal static class IdentityAdministrationAccessResolver
{
    public static async Task<TAccess> ResolveAsync<TAccess>(
        IRemoteFeatureProvider remoteFeatureProvider,
        IIdentityPermissionContext permissionContext,
        string resource,
        TAccess unavailable,
        TAccess forbidden,
        Func<bool, bool, bool, TAccess> ready,
        CancellationToken cancellationToken)
    {
        if (!await remoteFeatureProvider.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return unavailable;

        var snapshot = await permissionContext.GetAsync(cancellationToken);
        if (snapshot.State == IdentityPermissionSnapshotState.Unavailable)
            return unavailable;

        if (snapshot.State == IdentityPermissionSnapshotState.Forbidden ||
            !snapshot.HasPermission(resource, IdentityPermissions.View))
            return forbidden;

        return ready(
            snapshot.HasPermission(resource, IdentityPermissions.Create),
            snapshot.HasPermission(resource, IdentityPermissions.Update),
            snapshot.HasPermission(resource, IdentityPermissions.Delete));
    }
}
