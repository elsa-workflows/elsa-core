using Elsa.Authorization;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;

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
    public static void RequirePermission(EndpointDefinition definition, string resource, string verb)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
        {
            definition.AllowAnonymous();
            return;
        }

        var permission = new Permission(resource, verb);

        EndpointPermissionRegistry.Record(definition.EndpointType, permission);
        definition.Options(x => x.RequireAuthorization(policy => policy.AddRequirements(new PermissionRequirement(permission))));
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
            definition.Permissions(new[] { PermissionNames.All }.Concat(permissions).ToArray());
    }
}
