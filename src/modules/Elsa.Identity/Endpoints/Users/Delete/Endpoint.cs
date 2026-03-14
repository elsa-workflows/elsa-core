using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Users.Delete;

/// <summary>
/// An endpoint that deletes a user by ID. Requires the <code>SecurityRoot</code> policy.
/// </summary>
[PublicAPI]
internal class Delete(IUserStore userStore) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/identity/users/{id}");
        ConfigurePermissions("delete:user");
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;

        var user = await userStore.FindAsync(new UserFilter { Id = id }, cancellationToken);

        if (user == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await userStore.DeleteAsync(new UserFilter { Id = id }, cancellationToken);
        await Send.NoContentAsync(cancellationToken);
    }
}
