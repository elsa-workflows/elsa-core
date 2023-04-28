using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Users.Create;

/// <summary>
/// An endpoint that creates a new user. Requires the <code>SecurityRoot</code> policy.
/// </summary>
[PublicAPI]
internal class Create : ElsaEndpoint<Request, Response>
{
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISecretGenerator _secretGenerator;
    private readonly ISecretHasher _secretHasher;
    private readonly IUserStore _userStore;
    private readonly IRoleStore _roleStore;

    public Create(
        IIdentityGenerator identityGenerator,
        ISecretGenerator secretGenerator,
        ISecretHasher secretHasher,
        IUserStore userStore,
        IRoleStore roleStore)
    {
        _identityGenerator = identityGenerator;
        _secretGenerator = secretGenerator;
        _secretHasher = secretHasher;
        _userStore = userStore;
        _roleStore = roleStore;
    }

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
        var id = _identityGenerator.GenerateId();
        var password = string.IsNullOrWhiteSpace(request.Password) ? _secretGenerator.Generate() : request.Password.Trim();
        var hashedPassword = _secretHasher.HashSecret(password);

        var user = new User
        {
            Id = id,
            Name = request.Name,
            Roles = request.Roles ?? new List<string>(),
            HashedPassword = hashedPassword.EncodeSecret(),
            HashedPasswordSalt = hashedPassword.EncodeSalt()
        };

        await _userStore.SaveAsync(user, cancellationToken);

        var response = new Response(
            id,
            user.Name,
            password,
            user.Roles,
            hashedPassword.EncodeSecret(),
            hashedPassword.EncodeSalt());

        await SendOkAsync(response, cancellationToken);
    }
}