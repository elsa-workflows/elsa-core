using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Roles.Update;

/// <summary>
/// An endpoint that updates an existing role.
/// </summary>
[PublicAPI]
internal class Update(IRoleStore roleStore, IRoleAuthorizationService roleAuthorizationService, IPermissionGrantValidator grantValidator, Services.RoleSecurityNotifier securityNotifier) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Put("/identity/roles/{id}");
        RequirePermission(Elsa.Identity.Permissions.IdentityPermissions.Roles, CoreVerbs.Update);
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

        var validation = grantValidator.Validate(request.Permissions);

        if (!validation.IsValid)
        {
            // Both parts matter: the permission identifies which entry to fix, the reason says how.
            foreach (var error in validation.Errors)
                AddError($"{error.Permission} — {error.Reason}");

            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        if (!roleAuthorizationService.CanMutateRole(User, role, request.Permissions))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            role.Name = request.Name.Trim();

        if (request.Permissions != null)
            role.Permissions = request.Permissions;

        await roleStore.SaveAsync(role, cancellationToken);
        await securityNotifier.RoleChangedAsync(User, "updated", role.Id, role.Name, role.Permissions.ToArray(), cancellationToken);

        var response = new Response(
            role.Id,
            role.Name,
            role.Permissions,
            role.TenantId);

        await Send.OkAsync(response, cancellationToken);
    }
}
