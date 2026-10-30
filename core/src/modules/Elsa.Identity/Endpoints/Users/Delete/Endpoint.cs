using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Identity.Endpoints.Users.Delete;

/// <summary>
/// An endpoint that deletes a user by ID.
/// </summary>
[PublicAPI]
internal class Delete(IUserDeletionCoordinator coordinator, IUserStore userStore, IRoleAuthorizationService roleAuthorizationService, IRoleProvider roleProvider) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/identity/users/{id}");
        RequirePermission(Elsa.Identity.Permissions.IdentityPermissions.Users, CoreVerbs.Delete);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;

        // The coordinator loads the user again inside its own deletion flow. The endpoint needs the user first because
        // the caller's authority depends on the target's current roles, and the coordinator has no notion of the caller.
        var user = await userStore.FindAsync(new() { Id = id }, cancellationToken);

        if (user == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        // Callers may only delete users whose current roles they could assign themselves.
        if (!await roleAuthorizationService.CanManageUserAsync(roleProvider, User, user, cancellationToken))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var result = await coordinator.DeleteAsync(id, cancellationToken);
        switch (result)
        {
            case UserDeletionOperationResult.Deleted:
                await Send.NoContentAsync(cancellationToken);
                break;
            case UserDeletionOperationResult.NotFound:
                await Send.NotFoundAsync(cancellationToken);
                break;
            case UserDeletionOperationResult.Blocked blocked:
                HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                await HttpContext.Response.WriteAsJsonAsync(
                    new { error = "conflict", message = "The user is referenced by one or more installed modules.", dependencies = blocked.Dependencies },
                    cancellationToken);
                break;
            default:
                throw new InvalidOperationException("Unknown user-deletion operation result.");
        }
    }
}
