using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Applications.Create;

/// <summary>
/// An endpoint that creates a new application.
/// </summary>
[PublicAPI]
internal class Create(
    IIdentityGenerator identityGenerator,
    IClientIdGenerator clientIdGenerator,
    ISecretGenerator secretGenerator,
    IApiKeyGenerator apiKeyGenerator,
    ISecretHasher secretHasher,
    IApplicationStore applicationStore,
    IRoleStore roleStore)
    : ElsaEndpoint<Request, Response>
{
    private readonly IRoleStore _roleStore = roleStore;

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/identity/applications");
        RequirePermission(Elsa.Identity.Permissions.IdentityPermissions.Applications, CoreVerbs.Create);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var id = identityGenerator.GenerateId();
        var clientId = await clientIdGenerator.GenerateAsync(cancellationToken);
        var clientSecret = secretGenerator.Generate();
        var hashedClientSecret = secretHasher.HashSecret(clientSecret);
        var apiKey = apiKeyGenerator.Generate(clientId);
        var hashedApiKey = secretHasher.HashSecret(apiKey);

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

        await applicationStore.SaveAsync(application, cancellationToken);

        var response = new Response(
            id, 
            application.Name, 
            application.Roles, 
            clientId,
            clientSecret,
            apiKey,
            hashedApiKey.EncodeSecret(),
            hashedApiKey.EncodeSalt(),
            hashedClientSecret.EncodeSecret(),
            hashedClientSecret.EncodeSalt());
        
        await Send.OkAsync(response, cancellationToken);
    }
}