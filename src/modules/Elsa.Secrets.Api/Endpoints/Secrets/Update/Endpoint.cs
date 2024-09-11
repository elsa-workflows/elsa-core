using Elsa.Abstractions;
using Elsa.Common.Contracts;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Update;

/// Updates an agent.
[UsedImplicitly]
public class Endpoint(ISecretManager agentManager, ISystemClock systemClock) : ElsaEndpoint<SecretInputModel, SecretModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/secrets/{id}");
        ConfigurePermissions("secrets:write");
    }

    /// <inheritdoc />
    public override async Task<SecretModel> ExecuteAsync(SecretInputModel req, CancellationToken ct)
    {
        var id = Route<string>("id")!;
        var entity = await agentManager.GetAsync(id, ct);

        if (entity == null)
        {
            await SendNotFoundAsync(ct);
            return null!;
        }

        var isNameDuplicate = await IsNameDuplicateAsync(req.Name, id, ct);

        if (isNameDuplicate)
        {
            AddError("Another secret already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return entity.ToModel();
        }

        entity.Name = req.Name.Trim();
        entity.Description = req.Description.Trim();
        entity.Algorithm = req.Algorithm;
        entity.Type = req.Type;
        entity.EncryptedValue = req.EncryptedValue;
        entity.IV = req.IV;
        entity.ExpiresAt = req.ExpiresAt;
        entity.RotationPolicy = req.RotationPolicy;
        entity.EncryptionKeyId = req.EncryptionKeyId;
        entity.UpdatedAt = systemClock.UtcNow;

        await agentManager.UpdateAsync(entity, ct);
        return entity.ToModel();
    }

    private async Task<bool> IsNameDuplicateAsync(string name, string id, CancellationToken cancellationToken)
    {
        return !await agentManager.IsNameUniqueAsync(name, id, cancellationToken);
    }
}