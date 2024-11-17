using Elsa.Abstractions;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.GetInputModel;

/// <summary>
/// Gets a secret.
/// </summary>
[UsedImplicitly]
public class Endpoint(ISecretManager agentManager, ISecretEncryptor secretEncryptor) : ElsaEndpoint<Request, SecretInputModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/secrets/{id}/input");
        ConfigurePermissions("secrets:read");
    }

    /// <inheritdoc />
    public override async Task<SecretInputModel> ExecuteAsync(Request req, CancellationToken ct)
    {
        var entity = await agentManager.GetAsync(req.Id, ct);
        
        if(entity == null)
        {
            await SendNotFoundAsync(ct);
            return null!;
        }

        var value = await secretEncryptor.DecryptAsync(entity, ct);
        return entity.ToInputModel(value);
    }
}