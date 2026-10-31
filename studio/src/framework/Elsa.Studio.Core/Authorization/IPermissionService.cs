namespace Elsa.Studio.Authorization;

/// <summary>
/// Resolves the current user's permissions so Studio can hide navigation, pages and actions the user cannot use.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Returns the current user's permissions. Production adapters fail closed with a known empty set
    /// when the snapshot is unavailable. <see cref="UserPermissions.Unknown"/> is only for hosts that
    /// have not registered an adapter.
    /// </summary>
    ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken = default);
}
