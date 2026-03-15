using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Roles.List;

/// <summary>
/// An endpoint that lists all roles.
/// </summary>
[PublicAPI]
internal class List(IRoleStore roleStore) : ElsaEndpointWithoutRequest<Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/identity/roles");
        ConfigurePermissions("read:role");
    }

    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var roles = await roleStore.FindManyAsync(new(), cancellationToken);

        var response = new Response(roles
            .Select(role => new RoleSummary(role.Id, role.Name, role.Permissions, role.TenantId))
            .ToList());

        return response;
    }
}