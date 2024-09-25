using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Services.Update;

/// Lists all registered API keys.
[UsedImplicitly]
public class Endpoint(IServiceStore store) : ElsaEndpoint<ServiceModel, ServiceModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/services/{id}");
        ConfigurePermissions("ai/services:write");
    }

    /// <inheritdoc />
    public override async Task<ServiceModel> ExecuteAsync(ServiceModel req, CancellationToken ct)
    {
        var entity = await store.GetAsync(req.Id, ct);
        
        if(entity == null)
        {
            await SendNotFoundAsync(ct);
            return null!;
        }
        
        var isNameDuplicate = await IsNameDuplicateAsync(req.Name, req.Id, ct);

        if (isNameDuplicate)
        {
            AddError("Another service already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return entity.ToModel();
        }

        entity.Name = req.Name.Trim();
        entity.Type = req.Type.Trim();
        entity.Settings = req.Settings;

        await store.UpdateAsync(entity, ct);
        return entity.ToModel();
    }
    
    private async Task<bool> IsNameDuplicateAsync(string name, string id, CancellationToken cancellationToken)
    {
        var filter = new ServiceDefinitionFilter
        {
            Name = name,
            NotId = id
        };

        var entity = await store.FindAsync(filter, cancellationToken);
        return entity != null;
    }
}