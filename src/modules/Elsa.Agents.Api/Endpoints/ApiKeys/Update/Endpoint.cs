using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.ApiKeys.Update;

/// Lists all registered API keys.
[UsedImplicitly]
public class Endpoint(IApiKeyStore store) : ElsaEndpoint<Request, ApiKeyDefinition>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/agents/{id}");
        ConfigurePermissions("ai/api-keys:write");
    }

    /// <inheritdoc />
    public override async Task<ApiKeyDefinition> ExecuteAsync(Request req, CancellationToken ct)
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
            AddError("Another API key already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return entity;
        }

        entity.Name = req.Name.Trim();
        entity.Value = req.Value.Trim();

        await store.UpdateAsync(entity, ct);
        return entity;
    }
    
    private async Task<bool> IsNameDuplicateAsync(string name, string id, CancellationToken cancellationToken)
    {
        var filter = new ApiKeyDefinitionFilter
        {
            Name = name,
            NotId = id
        };

        var entity = await store.FindAsync(filter, cancellationToken);
        return entity != null;
    }
}