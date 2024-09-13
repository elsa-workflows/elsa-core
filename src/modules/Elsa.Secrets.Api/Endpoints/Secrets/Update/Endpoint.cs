using Elsa.Abstractions;
using Elsa.Common.Contracts;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Update;

/// Updates an agent.
[UsedImplicitly]
public class Endpoint(ISecretManager manager, ISecretEncryptor secretEncryptor) : ElsaEndpoint<SecretInputModel, SecretModel>
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
        var entity = await manager.GetAsync(id, ct);

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

        await secretEncryptor.EncryptAsync(entity, req, ct);

        await manager.UpdateAsync(entity, ct);
        return entity.ToModel();
    }

    private async Task<bool> IsNameDuplicateAsync(string name, string id, CancellationToken cancellationToken)
    {
        return !await manager.IsNameUniqueAsync(name, id, cancellationToken);
    }
}