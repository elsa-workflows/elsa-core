using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Services.Create;

/// <summary>
/// Lists all registered API keys.
/// </summary>
[UsedImplicitly]
public class Endpoint(IServiceStore store, IIdentityGenerator identityGenerator) : ElsaEndpoint<ServiceInputModel, ServiceModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/services");
        ConfigurePermissions("ai/services:write");
    }

    /// <inheritdoc />
    public override async Task<ServiceModel> ExecuteAsync(ServiceInputModel req, CancellationToken ct)
    {
        var existingEntityFilter = new ServiceDefinitionFilter
        {
            Name = req.Name
        };
        var existingEntity = await store.FindAsync(existingEntityFilter, ct);

        if (existingEntity != null)
        {
            AddError("A Service already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return existingEntity.ToModel();
        }

        var newEntity = new ServiceDefinition
        {
            Id = identityGenerator.GenerateId(),
            Name = req.Name.Trim(),
            Type = req.Type,
            Settings = req.Settings
        };

        await store.AddAsync(newEntity, ct);
        return newEntity.ToModel();
    }
}