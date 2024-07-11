using Elsa.Abstractions;
using Elsa.Workflows.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

[PublicAPI]
internal class List(IActivityRegistry registry) : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/descriptors/activities");
        ConfigurePermissions("read:*", "read:activity-descriptors");
    }

    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = registry.ListAll().ToList();
        var response = new Response(descriptors);

        return Task.FromResult(response);
    }
}