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
        
        if (string.IsNullOrWhiteSpace(type))
        {
            AddError("ActivityType is required");
            await SendErrorsAsync(cancellation: ct);
            return;
        }
        
        var config = await store.GetConnectionDescriptorAsync(type, ct);
        await SendOkAsync(config, ct);
    }
}
