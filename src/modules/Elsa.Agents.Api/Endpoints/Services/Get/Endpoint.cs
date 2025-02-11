using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Services.Get;

/// <summary>
/// Lists all registered API keys.
/// </summary>
[UsedImplicitly]
public class Endpoint(IServiceStore store) : ElsaEndpoint<Request, ServiceModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/services/{id}");
        ConfigurePermissions("ai/services:read");
    }

    /// <inheritdoc />
    public override async Task<ServiceModel> ExecuteAsync(Request req, CancellationToken ct)
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