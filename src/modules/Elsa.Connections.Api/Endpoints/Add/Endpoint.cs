using Elsa.Abstractions;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Connections.Persistence.Filters;
using Elsa.Workflows;
using Elsa.Connections.Persistence.Entities;
using Elsa.Connections.Api.Extensions;

namespace Elsa.Connections.Api.Endpoints.Add;

public class Endpoint(IConnectionStore store, IIdentityGenerator identityGenerator) : ElsaEndpoint<ConnectionInputModel, ConnectionModel>
{
    public override void Configure()
    {
        Post("/connection-configuration");
        ConfigurePermissions($"{Constants.PermissionsNamespace}:write");
    }

    public override async Task<ConnectionModel> ExecuteAsync(ConnectionInputModel model, CancellationToken ct)
    {
        var isNameUnique = await IsNameUniqueAsync(model.Name, ct);

        if (!isNameUnique)
        {
            AddError("A connection already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return null!;
        }

        var newEntity = new ConnectionDefinition
        {
            Id = identityGenerator.GenerateId(),
            Name = model.Name,
            Description = model.Description,
            ConnectionType = model.ConnectionType,
            ConnectionConfiguration = model.ConnectionConfiguration,
        };
        await store.AddAsync(newEntity, ct);
        await SendOkAsync(ct);

        return newEntity.ToModel();
    }

    private async Task<bool> IsNameUniqueAsync(string name, CancellationToken ct)
    {
        var filter = new ConnectionDefinitionFilter
        {
            Name = name
        };
        return await store.FindAsync(filter, ct) == null;
    }
}

