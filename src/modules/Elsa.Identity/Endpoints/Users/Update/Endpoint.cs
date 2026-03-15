using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Users.Update;

/// <summary>
/// An endpoint that updates an existing user's password and roles.
/// </summary>
[PublicAPI]
internal class Update(IUserStore userStore, ISecretHasher secretHasher) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Put("/identity/users/{id}");
        ConfigurePermissions("update:user");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;

        var user = await userStore.FindAsync(new()
            { Id = id }, cancellationToken);

        if (user == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (request.Roles != null)
            user.Roles = request.Roles;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var hashedPassword = secretHasher.HashSecret(request.Password.Trim());
            user.HashedPassword = hashedPassword.EncodeSecret();
            user.HashedPasswordSalt = hashedPassword.EncodeSalt();
        }

        await userStore.SaveAsync(user, cancellationToken);

        var response = new Response(
            user.Id,
            user.Name,
            user.Roles,
            user.TenantId);

        await Send.OkAsync(response, cancellationToken);
    }
}
