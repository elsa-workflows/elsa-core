using Elsa.Identity.Contracts;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Secrets.Hash;

/// <summary>
/// Hash a given password. Requires the <code>SecurityRoot</code> policy.
/// </summary>
[PublicAPI]
internal class Hash : Endpoint<Request, Response>
{
    private readonly ISecretHasher _secretHasher;

    public Hash(ISecretHasher secretHasher)
    {
        _secretHasher = secretHasher;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/secrets/hash");
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var hashedPassword = _secretHasher.HashSecret(request.Secret);
        var response = new Response(hashedPassword.EncodeSecret(), hashedPassword.EncodeSalt());

        return Task.FromResult(response);
    }
}