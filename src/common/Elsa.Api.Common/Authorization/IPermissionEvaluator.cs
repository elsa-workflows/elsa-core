using System.Security.Claims;

namespace Elsa.Authorization;

/// <summary>
/// The single place permission decisions are made. Every enforcement path — endpoint authorization,
/// SignalR hubs, and in-service checks — routes through this, so there is one implementation to audit.
/// </summary>
/// <remarks>
/// This covers <em>permission</em> decisions only. Authorization concerns that are not permission checks,
/// notably deployment read-only mode, are a separate axis and keep their own enforcement.
/// </remarks>
public interface IPermissionEvaluator
{
    /// <summary>Whether <paramref name="principal"/> holds a permission satisfying <paramref name="required"/>.</summary>
    bool HasPermission(ClaimsPrincipal? principal, Permission required);

    /// <summary>Whether <paramref name="principal"/> holds a permission satisfying <paramref name="resource"/> and <paramref name="verb"/>.</summary>
    bool HasPermission(ClaimsPrincipal? principal, string resource, string verb);

    /// <summary>Whether <paramref name="principal"/> holds a permission satisfying every one of <paramref name="required"/>.</summary>
    bool HasAllPermissions(ClaimsPrincipal? principal, IEnumerable<Permission> required);

    /// <summary>
    /// The permissions <paramref name="principal"/> holds, as the union across all of its roles. Malformed
    /// claim values are skipped rather than throwing, so one bad stored grant cannot deny an entire principal.
    /// </summary>
    IReadOnlyCollection<Permission> GetGrants(ClaimsPrincipal? principal);
}
