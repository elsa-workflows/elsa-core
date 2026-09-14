using System.Security.Claims;
using Elsa.Authorization;

namespace Elsa.ExternalAuthentication.Permissions;

/// <summary>
/// Evaluates permissions the module carries as configured or delegated <em>strings</em> rather than as a
/// resource and verb pair.
/// </summary>
/// <remarks>
/// These are the call sites that used to compare claim values with ordinal equality, which made a wildcard
/// grant mean one thing on an endpoint and another here. Routing them through <see cref="IPermissionEvaluator"/>
/// leaves one matching rule for the whole module.
/// </remarks>
internal static class PermissionEvaluatorExtensions
{
    /// <summary>
    /// Whether <paramref name="actor"/> holds a permission satisfying <paramref name="permission"/>. A value
    /// that is not a well-formed permission is held by nobody, so a malformed setting fails closed.
    /// </summary>
    public static bool HasPermission(this IPermissionEvaluator evaluator, ClaimsPrincipal? actor, string permission) =>
        Permission.TryParse(permission, out var required) && evaluator.HasPermission(actor, required);
}
