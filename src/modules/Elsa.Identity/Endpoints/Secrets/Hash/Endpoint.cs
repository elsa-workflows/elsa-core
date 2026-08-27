using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Secrets.Hash;

/// <summary>
/// Hash a given password. Requires <c>identity/users:create</c>.
/// </summary>
/// <remarks>
/// This endpoint previously carried only the <c>SecurityRoot</c> policy, which by default resolved to nothing
/// more than "any authenticated caller" -- so any signed-in user could exercise the password hasher. It is
/// declared against identity/users:create because hashing a secret is a step in provisioning a credential:
/// the callers that need it are the ones standing up users and applications. See ADR 0010.
/// </remarks>
[PublicAPI]
internal class Hash(ISecretHasher secretHasher) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/secrets/hash");
        RequirePermission(Elsa.Identity.Permissions.IdentityPermissions.Users, CoreVerbs.Create);
    }

    /// <inheritdoc />
    public override Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var hashedPassword = secretHasher.HashSecret(request.Secret);
        var response = new Response(hashedPassword.EncodeSecret(), hashedPassword.EncodeSalt());

        return Task.FromResult(response);
    }
}
