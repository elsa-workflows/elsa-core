using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Me.Permissions;

/// <summary>
/// Returns the calling principal's effective grants, so a client can hide sections, disable actions and
/// show read-only states from one call rather than probing endpoints.
/// </summary>
/// <remarks>
/// This is for rendering. The source of truth is always server-side: every protected endpoint
/// re-evaluates independently, and this response is never an authorization decision.
/// </remarks>
[PublicAPI]
internal class Get(IPermissionDescriptorRegistry registry, IPermissionEvaluator evaluator) : ElsaEndpointWithoutRequest<Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/identity/me/permissions");

        // Any authenticated principal may ask what it holds; no grant is required to see your own.
        RequireAuthenticatedOnly();
    }

    /// <inheritdoc />
    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var grants = registry.List()
            .Select(descriptor => new ResourceGrant(
                descriptor.Resource,
                descriptor.SupportedVerbs.Where(verb => evaluator.HasPermission(User, descriptor.Resource, verb)).ToArray()))
            .ToArray();

        return Task.FromResult(new Response(grants));
    }
}
