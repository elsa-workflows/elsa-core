using Elsa.Abstractions;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Connections.Api.Extensions;

namespace Elsa.Connections.Api.Endpoints.Get;

public class Endpoint(IConnectionStore store) : ElsaEndpoint<Request,ConnectionModel>
{
    public override void Configure()
    {
        Get("/connection-configuration/{id}");
        ConfigurePermissions($"{Constants.PermissionsNamespace}:read");
    }

    public override async Task<ConnectionModel> ExecuteAsync(Request req, CancellationToken ct)
    {
        var entity = await store.FindAsync(
            new()
            {
                Id = req.Id
            }, ct);

        if (entity == null) 
        {
            await SendNotFoundAsync(ct);
            return null!;
        }

        return entity.ToModel();
    }
}