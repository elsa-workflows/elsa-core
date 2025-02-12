using Elsa.Abstractions;
using Elsa.Connections.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Connections.Api.Endpoints.ActivityConnectionDescriptor.Get;

public class Endpoint(IConnectionDescriptorRegistry store) : ElsaEndpointWithoutRequest<IEnumerable<InputDescriptor>>
{
    public override void Configure()
    {
        Get("/connection-configuration/input-descriptor/{ActivityType}");
        ConfigurePermissions($"{Constants.PermissionsNamespace}/descriptor:read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var type = Route<string>("ActivityType");
        var config = await store.GetConnectionDescriptor(type);

        if (config == null)
            await SendNotFoundAsync();
        else
            await SendOkAsync(config);

    }
}
