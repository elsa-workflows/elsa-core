using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Roles.Delete;

/// <summary>Returns the current cross-module impact of deleting a role.</summary>
[PublicAPI]
internal sealed class GetDeletionImpact(IRoleDeletionCoordinator coordinator) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/identity/roles/{id}/deletion-impact");
        ConfigurePermissions("delete:role");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await coordinator.InspectAsync(Route<string>("id")!, User, cancellationToken);
        await RoleDeletionEndpointSupport.SendInspectionResultAsync(HttpContext, result, cancellationToken);
    }
}
