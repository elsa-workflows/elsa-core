using Elsa.Abstractions;
using Elsa.Connections.Contracts;


namespace Elsa.Connections.Api.Endpoints.Delete;

public class Endpoint(IConnectionRepository store) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/connection-configuration/{id}");
        AllowAnonymous();
    }

    public override async Task<object?> HandleAsync(CancellationToken ct)
    {
        var configurationId = Route<string>("id");
        await store.DeleteConnectionConfigurationAsync(configurationId, ct);
        await SendOkAsync();

        return null;
    }
}
