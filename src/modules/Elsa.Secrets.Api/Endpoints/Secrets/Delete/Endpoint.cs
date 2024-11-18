using Elsa.Abstractions;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Delete;

/// <summary>
/// Deletes a secret.
/// </summary>
[UsedImplicitly]
public class Endpoint(ISecretManager manager) : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/secrets/{id}");
        ConfigurePermissions("secrets:delete");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var entity = await manager.GetAsync(req.Id, ct);
        
        if(entity == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }
        
        await manager.DeleteAsync(entity, ct);
    }
}