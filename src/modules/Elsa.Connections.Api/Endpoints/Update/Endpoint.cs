using Elsa.Abstractions;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Connections.Persistence.Filters;
using Elsa.Connections.Api.Extensions;

namespace Elsa.Connections.Api.Endpoints.Update;

public class Endpoint(IConnectionStore store) : ElsaEndpoint<ConnectionInputModel, ConnectionModel>
{
    public override void Configure()
    {
        Put("/connection-configuration/{id}");
        ConfigurePermissions($"{Constants.PermissionsNamespace}:write");
    }

    public override async Task<ConnectionModel> ExecuteAsync(ConnectionInputModel model, CancellationToken ct)
    {
        var id = Route<string>("id")!;
        var entity = await store.GetAsync(id, ct);

        if (entity == null)
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

