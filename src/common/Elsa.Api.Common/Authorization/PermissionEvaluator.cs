using System.Security.Claims;

namespace Elsa.Authorization;

/// <inheritdoc />
public sealed class PermissionEvaluator : IPermissionEvaluator
{
    /// <inheritdoc />
    public bool HasPermission(ClaimsPrincipal? principal, Permission required) =>
        principal is not null && EnumerateGrants(principal).Any(granted => PermissionMatcher.Satisfies(granted, required));

    /// <inheritdoc />
    public bool HasPermission(ClaimsPrincipal? principal, string resource, string verb) =>
        HasPermission(principal, new Permission(resource, verb));

    /// <inheritdoc />
    public bool HasAllPermissions(ClaimsPrincipal? principal, IEnumerable<Permission> required)
    {
        var grants = GetGrants(principal);
        return required.All(x => PermissionMatcher.Satisfies(grants, x));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Permission> GetGrants(ClaimsPrincipal? principal) =>
        principal is null ? [] : EnumerateGrants(principal).Distinct().ToArray();

    private static IEnumerable<Permission> EnumerateGrants(ClaimsPrincipal principal)
    {
        foreach (var claim in principal.FindAll(PermissionNames.ClaimType))
        {
            if (Permission.TryParse(claim.Value, out var permission))
                yield return permission;
        }
    }
}
