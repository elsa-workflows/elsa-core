using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Roles.Delete;

/// <summary>
/// An endpoint that deletes a role by ID.
/// </summary>
[PublicAPI]
internal class Delete(IRoleStore roleStore) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/identity/roles/{id}");
        ConfigurePermissions("delete:role");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;

        var role = await roleStore.FindAsync(new()
        {
            Id = id
        }, cancellationToken);

        if (role == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await roleStore.DeleteAsync(new()
        {
            Id = id
        }, cancellationToken);
        await Send.NoContentAsync(cancellationToken);
    }
}