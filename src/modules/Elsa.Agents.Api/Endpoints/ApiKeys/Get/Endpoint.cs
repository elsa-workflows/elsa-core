using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.ApiKeys.Get;

/// Lists all registered API keys.
[UsedImplicitly]
public class Endpoint(IApiKeyStore store) : ElsaEndpoint<Request, ApiKeyModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/api-keys/{id}");
        ConfigurePermissions("ai/api-keys:read");
    }

    /// <inheritdoc />
    public override async Task<ApiKeyModel> ExecuteAsync(Request req, CancellationToken ct)
    {
        var entity = await store.GetAsync(req.Id, ct);
        
        if(entity == null)
        {
            await SendNotFoundAsync(ct);
            return null!;
        }
        
        return entity.ToModel();
    }
}