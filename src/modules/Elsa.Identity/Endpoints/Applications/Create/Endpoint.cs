using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Workflows.Core.Contracts;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Applications.Create;

/// <summary>
/// An endpoint that creates a new application. Requires the <code>SecurityRoot</code> policy.
/// </summary>
[PublicAPI]
internal class Create : Endpoint<Request, Response>
{
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IClientIdGenerator _clientIdGenerator;
    private readonly ISecretGenerator _secretGenerator;
    private readonly IApiKeyGenerator _apiKeyGenerator;
    private readonly ISecretHasher _secretHasher;
    private readonly IApplicationStore _applicationStore;
    private readonly IRoleStore _roleStore;

    public Create(
        IIdentityGenerator identityGenerator,
        IClientIdGenerator clientIdGenerator,
        ISecretGenerator secretGenerator,
        IApiKeyGenerator apiKeyGenerator,
        ISecretHasher secretHasher,
        IApplicationStore applicationStore,
        IRoleStore roleStore)
    {
        _identityGenerator = identityGenerator;
        _clientIdGenerator = clientIdGenerator;
        _secretGenerator = secretGenerator;
        _apiKeyGenerator = apiKeyGenerator;
        _secretHasher = secretHasher;
        _applicationStore = applicationStore;
        _roleStore = roleStore;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/applications");
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var id = _identityGenerator.GenerateId();
        var clientId = await _clientIdGenerator.GenerateAsync(cancellationToken);
        var clientSecret = _secretGenerator.Generate();
        var hashedClientSecret = _secretHasher.HashSecret(clientSecret);
        var apiKey = _apiKeyGenerator.Generate(clientId);
        var hashedApiKey = _secretHasher.HashSecret(apiKey);

        var application = new Application
        {
            Id = id,
            ClientId = clientId,
            HashedClientSecret = hashedClientSecret.EncodeSecret(),
            HashedClientSecretSalt = hashedClientSecret.EncodeSalt(),
            Name = request.Name,
            HashedApiKey = hashedApiKey.EncodeSecret(),
            HashedApiKeySalt = hashedApiKey.EncodeSalt(),
            Roles = request.Roles ?? new List<string>()
        };

        await _applicationStore.SaveAsync(application, cancellationToken);

        var response = new Response(apiKey);
        await SendOkAsync(response, cancellationToken);
    }
}