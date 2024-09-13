using Elsa.Abstractions;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Create;

[UsedImplicitly]
internal class Endpoint(ISecretManager manager, ISecretEncryptor secretEncryptor) : ElsaEndpoint<SecretInputModel, SecretModel>
{
    public override void Configure()
    {
        Post("/secrets");
        ConfigurePermissions("write:secrets");
    }

    public override async Task<SecretModel> ExecuteAsync(SecretInputModel request, CancellationToken cancellationToken)
    {
        var secret = await secretEncryptor.EncryptAsync(request, cancellationToken);
        await manager.AddAsync(secret, cancellationToken);
        var secretModel = secret.ToModel();
        return secretModel;
    }
}