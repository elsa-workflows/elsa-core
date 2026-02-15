using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Users.Create;

/// <summary>
/// An endpoint that creates a new user. Requires the <code>SecurityRoot</code> policy.
/// </summary>
[PublicAPI]
internal class Create(IUserManager userManager)
    : ElsaEndpoint<Request, Response>
{

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/users");
        ConfigurePermissions("create:user");
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await userManager.CreateUserAsync(
            request.Name,
            request.Password,
            request.Roles,
            cancellationToken);

        var response = new Response(
            result.User.Id,
            result.User.Name,
            result.Password,
            result.User.Roles,
            result.User.TenantId,
            result.User.HashedPassword,
            result.User.HashedPasswordSalt);

        await Send.OkAsync(response, cancellationToken);
    }
}