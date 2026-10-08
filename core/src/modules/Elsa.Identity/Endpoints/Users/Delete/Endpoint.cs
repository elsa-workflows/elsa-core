using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Identity.Endpoints.Users.Delete;

/// <summary>
/// An endpoint that deletes a user by ID.
/// </summary>
[PublicAPI]
internal class Delete(IUserDeletionCoordinator coordinator) : ElsaEndpointWithoutRequest
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
