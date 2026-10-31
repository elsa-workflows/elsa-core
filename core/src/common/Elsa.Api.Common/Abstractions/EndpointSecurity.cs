using Elsa.Authorization;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Elsa.Abstractions;

/// <summary>
/// The one implementation behind every base class's security helpers. The base classes cannot share a
/// common ancestor of their own, so the logic lives here rather than being copied six times.
/// </summary>
internal static class EndpointSecurity
{
    /// <summary>
    /// Requires a permission satisfying <paramref name="resource"/> and <paramref name="verb"/>. The
    /// requirement is attached as an inline policy so it needs no separate policy registration, and it is
    /// evaluated by <see cref="IPermissionEvaluator"/> like every other permission decision.
    /// </summary>
    public static void RequirePermission(EndpointDefinition definition, string resource, string verb) => RequireAnyPermission(definition, (resource, verb));

    /// <summary>
    /// Requires a permission satisfying any one of <paramref name="permissions"/>: an anonymous caller is challenged
    /// (401) and a signed-in caller holding none of them is forbidden (403). <see cref="RequirePermission"/> is the
    /// case of exactly one, and the two share everything else: the evaluator, the record in
    /// <see cref="EndpointPermissionRegistry"/>, and <see cref="EndpointSecurityOptions.SecurityIsEnabled"/>, which is
    /// read once, when the endpoint is configured, and when false allows every caller and records nothing.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="permissions"/> is empty.</exception>
    public static void RequireAnyPermission(EndpointDefinition definition, params (string Resource, string Verb)[] permissions)
    {
        // Built before the security check so an empty declaration fails even on a host that runs without security.
        var requirement = new EndpointPermissionRequirement(permissions.Select(x => new Permission(x.Resource, x.Verb)));

        if (!EndpointSecurityOptions.SecurityIsEnabled)
        {
            definition.AllowAnonymous();
            return;
        }

        EndpointPermissionRegistry.Record(definition.EndpointType, requirement);

        // Evaluated inline rather than through a registered IAuthorizationHandler. A handler would make
        // enforcement depend on the host having called AddElsaAuthorization: miss it, and every endpoint
        // answers 403 with nothing to indicate why. Hosts that wire FastEndpoints themselves -- several
        // test hosts among them -- do exactly that. The evaluator is stateless, so falling back to a shared
        // instance is safe, while a host that registers its own still wins.
        definition.Options(x => x.RequireAuthorization(policy => policy.RequireAssertion(context =>
        {
            var evaluator = (context.Resource as HttpContext)?.GetPermissionEvaluator() ?? PermissionEvaluator.Shared;

            return requirement.AnyOf.Any(permission => evaluator.HasPermission(context.User, permission));
        })));
    }

    /// <summary>
    /// Requires an authenticated caller but no permission. FR-019's third declaration state: it exists so
    /// that a deliberate "needs an identity, needs no grant" choice is distinguishable from an author who
    /// forgot to declare anything, which the coverage gate would otherwise have to treat alike.
    /// </summary>
    public static void RequireAuthenticatedOnly(EndpointDefinition definition)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
        {
            definition.AllowAnonymous();
            return;
        }

        definition.Options(x => x.RequireAuthorization(policy => policy.RequireAuthenticatedUser()));
    }

    /// <summary>The legacy string-based declaration, preserved for modules outside this repository.</summary>
    public static void ConfigurePermissions(EndpointDefinition definition, string[] permissions)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            definition.AllowAnonymous();
        else
            definition.Permissions([PermissionNames.All, .. permissions]);
    }
}
