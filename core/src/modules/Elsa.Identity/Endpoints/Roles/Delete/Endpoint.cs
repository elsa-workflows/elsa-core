using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Roles.Delete;

/// <summary>
/// An endpoint that deletes a role by ID.
/// </summary>
[PublicAPI]
internal class Delete(IRoleDeletionCoordinator coordinator) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/identity/roles/{id}");
        RequirePermission(Elsa.Identity.Permissions.IdentityPermissions.Roles, CoreVerbs.Delete);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;

        var result = await coordinator.DeleteAsync(id, User, cancellationToken);
        await RoleDeletionEndpointSupport.SendOperationResultAsync(HttpContext, result, cancellationToken);
    }
}
