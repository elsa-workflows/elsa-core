using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Identity.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Secrets.Hash;

/// <summary>
/// Hash a user password, returning the encoded hash and salt the user store would otherwise have written.
/// Requires <c>identity/users:create</c>. Scoped to user credentials only: application credentials are not
/// hashed here.
/// </summary>
/// <remarks>
/// This endpoint previously carried only the <c>SecurityRoot</c> policy, which by default resolved to nothing
/// more than "any authenticated caller" -- so any signed-in user could exercise the password hasher. It is
/// declared against identity/users:create because the credential it prepares is a user password: the caller
/// seeding a user out of band needs the same hash the user store would have written.
///
/// No application-provisioning flow reaches this endpoint, and none is documented to.
/// <c>POST /identity/applications</c> generates the client secret and the API key itself, hashes both, and
/// returns each plaintext alongside its hash, so <c>identity/applications:create</c> on its own stays
/// sufficient to create an application end to end. See ADR 0010.
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
