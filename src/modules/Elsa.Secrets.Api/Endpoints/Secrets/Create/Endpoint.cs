using Elsa.Abstractions;
using Elsa.Secrets.Management;
using Elsa.Workflows.Contracts;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Create;

[UsedImplicitly]
internal class Endpoint(ISecretManager manager, IIdentityGenerator identityGenerator) : ElsaEndpoint<SecretInputModel, SecretModel>
{
    public override void Configure()
    {
        Post("/secrets");
        ConfigurePermissions("write:secrets");
    }

    public override async Task<SecretModel> ExecuteAsync(SecretInputModel request, CancellationToken cancellationToken)
    {
        var newEntity = new Secret
        {
            Id = identityGenerator.GenerateId(),
            SecretId = identityGenerator.GenerateId(),
            Version = 1,
            Name = request.Name.Trim(),
            Type = request.Type?.Trim(),
            Description = request.Description.Trim(),
            EncryptedValue = request.EncryptedValue,
            IV = request.IV,
            EncryptionKeyId = request.EncryptionKeyId,
            Algorithm = request.Algorithm,
            ExpiresAt = request.ExpiresAt,
            RotationPolicy = request.RotationPolicy,
            Status = SecretStatus.Active
        };

        await manager.AddAsync(newEntity, cancellationToken);

        var secretModel = newEntity.ToModel();
        return secretModel;
    }
}