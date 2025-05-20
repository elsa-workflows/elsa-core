using Elsa.Abstractions;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Create;

[UsedImplicitly]
internal class Endpoint(ISecretManager manager) : ElsaEndpoint<SecretInputModel, SecretModel>
{
    public override void Configure()
    {
        Post("/secrets");
        ConfigurePermissions("write:secrets");
    }

    public override async Task<SecretModel> ExecuteAsync(SecretInputModel request, CancellationToken cancellationToken)
    {
        var secret = await manager.CreateAsync(request, cancellationToken);
        var secretModel = secret.ToModel();
        return secretModel;
    }
}