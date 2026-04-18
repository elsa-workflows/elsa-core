using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Roles.Update;

/// <summary>
/// An endpoint that updates an existing role.
/// </summary>
[PublicAPI]
internal class Update(IRoleStore roleStore) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Put("/identity/roles/{id}");
        ConfigurePermissions("update:role");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
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

        if (!string.IsNullOrWhiteSpace(request.Name))
            role.Name = request.Name.Trim();

        if (request.Permissions != null)
            role.Permissions = request.Permissions;

        await roleStore.SaveAsync(role, cancellationToken);

        var response = new Response(
            role.Id,
            role.Name,
            role.Permissions,
            role.TenantId);

        await Send.OkAsync(response, cancellationToken);
    }
}