using Elsa.Abstractions;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

public class List : ElsaEndpointWithoutRequest<Response>
{
    private readonly IActivityRegistry _registry;

    public List(IActivityRegistry registry)
    {
        _registry = registry;
    }

    public override void Configure()
    {
        Get("/descriptors/activities");
        ConfigurePermissions("read:*", "read:activity-descriptors");
    }

    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = _registry.ListAll().ToList();
        var response = new Response(descriptors);

        return Task.FromResult(response);
    }
}