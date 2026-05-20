using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Identity.Endpoints.Roles.Create;

/// <summary>
/// An endpoint that creates a new role.
/// </summary>
[PublicAPI]
internal class Create(IRoleManager roleManager, IRoleAuthorizationService roleAuthorizationService) : ElsaEndpoint<Request, Response>
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
        if (!roleAuthorizationService.CanCreateRoleWithPermissions(User, request.Permissions))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        CreateRoleResult result;
        try
        {
            result = await roleManager.CreateRoleAsync(
                request.Name,
                request.Permissions,
                request.Id,
                cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return;
        }

        var response = new Response(
            result.Role.Id,
            result.Role.Name,
            result.Role.Permissions);

        await Send.OkAsync(response, cancellationToken);
    }
}
