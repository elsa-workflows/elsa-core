using Elsa.Identity.Services;
using FastEndpoints;

namespace Elsa.Identity.Endpoints.Password.Hash;

/// <summary>
/// Hash a given password. Requires the <code>SecurityRoot</code> policy.
/// </summary>
public class Hash : Endpoint<Request, Response>
{
    private readonly IPasswordHasher _passwordHasher;

    public Hash(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/password/hash");
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var hashedPassword = _passwordHasher.HashPassword(request.Password);
        var response = new Response(hashedPassword);

        return Task.FromResult(response);
    }
}