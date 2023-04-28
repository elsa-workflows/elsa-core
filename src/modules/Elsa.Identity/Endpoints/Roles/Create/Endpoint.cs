using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Workflows.Core.Contracts;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Roles.Create;

/// <summary>
/// An endpoint that creates a new role.
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
        Post("/identity/roles");
        ConfigurePermissions("create:role");
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var id = request.Id ?? request.Name.Kebaberize();

        var role = new Role
        {
            Id = id,
            Name = request.Name,
            Permissions = request.Permissions ?? new List<string>()
        };

        await _roleStore.SaveAsync(role, cancellationToken);

        var response = new Response(
            id,
            role.Name,
            role.Permissions);

        await SendOkAsync(response, cancellationToken);
    }
}