using Elsa.Abstractions;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Connections.Persistence.Filters;
using Elsa.Workflows;
using Elsa.Connections.Persistence.Entities;
using Elsa.Connections.Api.Extensions;
using static FastEndpoints.Ep;

namespace Elsa.Connections.Api.Endpoints.Add;

public class Endpoint(IConnectionStore store, IIdentityGenerator identityGenerator) : ElsaEndpoint<ConnectionInputModel, ConnectionModel>
{
    public override void Configure()
    {
        Post("/connection-configuration");
        AllowAnonymous();
    }

    public override async Task<ConnectionModel> ExecuteAsync(ConnectionInputModel model, CancellationToken ct)
    {
        var isNameUnique = await IsNameUniqueAsync(model.Name, ct);

        if (!isNameUnique)
        {
            AddError("An connection already exists with the specified name");
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
        await SendOkAsync();

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

public class EndpointUpdate(IConnectionStore store) : ElsaEndpoint<ConnectionInputModel, ConnectionModel>
{
    public override void Configure()
    {
        Put("/connection-configuration/{id}");
        AllowAnonymous();
    }

    public override async Task<ConnectionModel> ExecuteAsync(ConnectionInputModel model, CancellationToken ct)
    {
        var id = Route<string>("id")!;
        var entity = await store.GetAsync(id, ct);

        if(entity == null)
        {
            await SendNotFoundAsync(ct);
            return null!;
        }

        var isNameDuplicate = await IsNameDuplicateAsync(model.Name, id, ct);

        if (isNameDuplicate)
        {
            AddError("Another connection already exist with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return entity.ToModel();
        }

        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.ConnectionConfiguration = model.ConnectionConfiguration;


        await store.UpdateAsync(entity, ct);
        
        return entity.ToModel();
    }

    private async Task<bool> IsNameDuplicateAsync(string name, string id, CancellationToken cancellationToken)
    {
        var entities = await store.FindAsync(new ConnectionDefinitionFilter
        {
            NotId = id,
            Name = name
        });

        return !(entities == null);
    }
}

