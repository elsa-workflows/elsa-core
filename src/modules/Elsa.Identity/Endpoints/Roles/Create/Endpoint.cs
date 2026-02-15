using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Roles.Create;

/// <summary>
/// An endpoint that creates a new role.
/// </summary>
[PublicAPI]
internal class Create(IRoleManager roleManager) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/roles");
        ConfigurePermissions("create:role");
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await roleManager.CreateRoleAsync(
            request.Name,
            request.Permissions,
            request.Id,
            cancellationToken);

        var response = new Response(
            result.Role.Id,
            result.Role.Name,
            result.Role.Permissions);

        await Send.OkAsync(response, cancellationToken);
    }
}