using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.ApiKeys.Create;

/// <summary>
/// Lists all registered API keys.
/// </summary>
[UsedImplicitly]
public class Endpoint(IApiKeyStore store, IIdentityGenerator identityGenerator) : ElsaEndpoint<ApiKeyInputModel, ApiKeyModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/api-keys");
        ConfigurePermissions("ai/api-keys:write");
    }

    /// <inheritdoc />
    public override async Task<ApiKeyModel> ExecuteAsync(ApiKeyInputModel req, CancellationToken ct)
    {
        var existingEntityFilter = new ApiKeyDefinitionFilter
        {
            Name = req.Name
        };
        var existingEntity = await store.FindAsync(existingEntityFilter, ct);

        if (existingEntity != null)
        {
            AddError("An API key already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return existingEntity.ToModel();
        }

        var newEntity = new ApiKeyDefinition
        {
            Id = identityGenerator.GenerateId(),
            Name = req.Name.Trim(),
            Value = req.Value.Trim()
        };

        await store.AddAsync(newEntity, ct);
        return newEntity.ToModel();
    }
}