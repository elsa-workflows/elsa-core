using Elsa.Abstractions;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Get;

/// Gets a secret.
[UsedImplicitly]
public class Endpoint(ISecretManager agentManager) : ElsaEndpoint<Request, SecretModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/secrets/{id}");
        ConfigurePermissions("secrets:read");
    }

    /// <inheritdoc />
    public override async Task<SecretModel> ExecuteAsync(Request req, CancellationToken ct)
    {
        var entity = await agentManager.GetAsync(req.Id, ct);
        
        if(entity == null)
        {
            await SendNotFoundAsync(ct);
            return null!;
        }
        
        return entity.ToModel();
    }
}