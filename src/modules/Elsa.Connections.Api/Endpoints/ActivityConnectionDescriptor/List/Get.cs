using Elsa.Abstractions;
using Elsa.Connections.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Connections.Api.Endpoints.ActivityConnectionDescriptor.List;

public class Get(IConnectionDescriptorRegistry store) : ElsaEndpointWithoutRequest<IEnumerable<InputDescriptor>>
{
    public override void Configure()
    {
        Get("/connection-configuration/input-descriptor/{ActivityType}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        string type = Route<string>("ActivityType");
        var config = await store.GetConnectionDescriptor(type);

        if (config == null)
            await SendNotFoundAsync();
        else
            await SendOkAsync(config);

    }
}
