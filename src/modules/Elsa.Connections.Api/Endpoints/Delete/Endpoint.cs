using Elsa.Abstractions;
using Elsa.Connections.Persistence.Contracts;


namespace Elsa.Connections.Api.Endpoints.Delete;

public class Endpoint(IConnectionStore store) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/connection-configuration/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var entity = await store.GetAsync(req.Id);

        if (entity == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }
        
        await store.DeleteAsync(entity, ct);
        await SendOkAsync();
    }
}
