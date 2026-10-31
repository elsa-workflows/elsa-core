using Elsa.Studio.Authorization;

namespace Elsa.Studio.ExternalAuthentication.Services;

public interface IExternalAuthenticationPermissionService
{
    ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Adapts <see cref="IPermissionService"/> so External Authentication affordances use the same
/// fail-closed <c>GET /identity/me/permissions</c> snapshot as the rest of Studio.
/// </summary>
public sealed class ExternalAuthenticationPermissionService(IPermissionService? permissions = null) : IExternalAuthenticationPermissionService
{
    /// <summary>Elsa's known-empty JWT sentinel (<c>PermissionNames.None</c> from #8567). Not a grant.</summary>
    private const string EmptySetSentinel = "none";

    public async ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (string.Equals(permission, EmptySetSentinel, StringComparison.Ordinal) ||
            !Permission.TryParse(permission, out var required))
        {
            return false;
        }

        return (await GetPermissionsAsync(cancellationToken)).Has(required);
    }

    public async ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetPermissionsAsync(cancellationToken);
        return user.Grants
            .Where(grant => !string.Equals(grant.Resource, EmptySetSentinel, StringComparison.Ordinal))
            .Select(grant => grant.ToString())
            .Where(value => !string.Equals(value, EmptySetSentinel, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
    }

    private async ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        // No IPermissionService (Security not installed): fail closed. IdentityPermissionService
        // never returns Unknown, so an expired or anonymous principal cannot widen grants.
        if (permissions == null)
        {
            return UserPermissions.FromGrants([]);
        }

        return await permissions.GetPermissionsAsync(cancellationToken);
    }
}
