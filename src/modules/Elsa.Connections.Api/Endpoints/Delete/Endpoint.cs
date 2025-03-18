﻿using Elsa.Abstractions;
using Elsa.Connections.Persistence.Contracts;

namespace Elsa.Connections.Api.Endpoints.Delete;

public class Endpoint(IConnectionStore store) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/connection-configuration/{id}");
        ConfigurePermissions($"{Constants.PermissionsNamespace}:delete");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var entity = await store.GetAsync(req.Id, ct);

        if (entity == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }
        
        await store.DeleteAsync(entity, ct);
        await SendOkAsync(ct);
    }
}
