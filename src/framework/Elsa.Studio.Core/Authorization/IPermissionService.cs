namespace Elsa.Studio.Authorization;

/// <summary>
/// Resolves the current user's permissions so Studio can hide navigation, pages and actions the user cannot use.
/// </summary>
public interface IPermissionService
{
    /// <summary>Returns the current user's permissions, or <see cref="UserPermissions.Unknown"/> when none are available.</summary>
    ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken = default);
}
