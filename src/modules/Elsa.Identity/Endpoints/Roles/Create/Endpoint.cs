using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Permissions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Identity.Endpoints.Roles.Create;

/// <summary>
/// An endpoint that creates a new role.
/// </summary>
[PublicAPI]
internal class Create(IRoleManager roleManager, IRoleAuthorizationService roleAuthorizationService, IPermissionGrantValidator grantValidator, Services.RoleSecurityNotifier securityNotifier) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/roles");
        RequirePermission(Elsa.Identity.Permissions.IdentityPermissions.Roles, CoreVerbs.Create);
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // Reject grants the catalog cannot account for before the anti-escalation check, so an author gets
        // a specific message rather than a blanket 403 for what is really a typo.
        var validation = grantValidator.Validate(request.Permissions);

        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                AddError($"{nameof(request.Permissions)}: {error.Permission}", error.Reason);

            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

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

        await securityNotifier.RoleChangedAsync(User, "created", result.Role.Id, result.Role.Name, result.Role.Permissions.ToArray(), cancellationToken);

        var response = new Response(
            result.Role.Id,
            result.Role.Name,
            result.Role.Permissions);

        await Send.OkAsync(response, cancellationToken);
    }
}
