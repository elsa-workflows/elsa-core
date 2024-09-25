using Elsa.Abstractions;
using Elsa.Common.Contracts;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Update;

/// Updates an agent.
[UsedImplicitly]
public class Endpoint(ISecretManager manager, ISecretNameValidator nameValidator) : ElsaEndpoint<SecretInputModel, SecretModel>
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

        var isNameDuplicate = !await nameValidator.IsNameUniqueAsync(req.Name, id, ct);

        if (isNameDuplicate)
        {
            AddError("Another secret already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return entity.ToModel();
        }
        
        var newVersion = await manager.UpdateAsync(entity, req, ct);
        return newVersion.ToModel();
    }
}