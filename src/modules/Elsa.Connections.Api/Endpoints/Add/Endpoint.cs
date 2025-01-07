using Elsa.Abstractions;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;

namespace Elsa.Connections.Api.Endpoints.Add;

public class Endpoint(IConnectionRepository store) : ElsaEndpoint<ConnectionConfigurationMetadataModel>
{
    public override void Configure()
    {
        Post("/connection-configuration");
        AllowAnonymous();
    }

    public override async Task<object?> ExecuteAsync(ConnectionConfigurationMetadataModel model, CancellationToken ct)
    {
        await store.AddConnectionConfigurationAsync(model, ct);
        await SendOkAsync();

        return null;
    }
}

public class EndpointUpdate(IConnectionRepository store) : ElsaEndpoint<ConnectionConfigurationMetadataModel>
{
    public override void Configure()
    {
        Put("/connection-configuration/{id}");
        AllowAnonymous();
    }

    public override async Task<object?> ExecuteAsync(ConnectionConfigurationMetadataModel model, CancellationToken ct)
    {
        await store.UpdateConnectionAsync(model.Id , model, ct);
        await SendOkAsync();

        return null;
    }
}

