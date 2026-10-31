using System.Security.Claims;

namespace Elsa.Authorization;

/// <inheritdoc />
public sealed class PermissionEvaluator : IPermissionEvaluator
{
    /// <summary>
    /// A shared instance for call sites that cannot reach dependency injection, such as SignalR hub
    /// helpers. The evaluator holds no state, so sharing one is safe.
    /// </summary>
    public static IPermissionEvaluator Shared { get; } = new PermissionEvaluator();

    /// <inheritdoc />
    public bool HasPermission(ClaimsPrincipal? principal, Permission required)
    {
        return principal is not null && EnumerateGrants(principal).Any(granted => PermissionMatcher.Satisfies(granted, required));
    }

    /// <inheritdoc />
    public bool HasPermission(ClaimsPrincipal? principal, string resource, string verb) => HasPermission(principal, new(resource, verb));

    /// <inheritdoc />
    public bool HasAllPermissions(ClaimsPrincipal? principal, IEnumerable<Permission> required)
    {
        var grants = GetGrants(principal);
        return required.All(x => PermissionMatcher.Satisfies(grants, x));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Permission> GetGrants(ClaimsPrincipal? principal)
    {
        return principal is null ? [] : EnumerateGrants(principal).Distinct().ToArray();
    }

    /// <remarks>
    /// Reads Elsa's own permission claim type. Elsa is the only authority that expands roles into
    /// permission claims (ADR 0009), and this model no longer uses the FastEndpoints permission mechanism,
    /// so its separately configurable claim type is not consulted.
    /// </remarks>
    private static IEnumerable<Permission> EnumerateGrants(ClaimsPrincipal principal)
    {
        foreach (var claim in principal.FindAll(PermissionNames.ClaimType))
        {
            if (Permission.TryParse(claim.Value, out var permission))
                yield return permission;
        }
    }
}
