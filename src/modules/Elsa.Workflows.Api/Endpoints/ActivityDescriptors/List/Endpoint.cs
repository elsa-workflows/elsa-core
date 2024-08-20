using Elsa.Abstractions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

[PublicAPI]
internal class List(IActivityRegistry registry, IActivityRegistryPopulator registryPopulator) : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/descriptors/activities");
        ConfigurePermissions("read:*", "read:activity-descriptors");
    }

    public override async Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var forceRefresh = Query<bool>("refresh", false);

        if (forceRefresh)
            await registryPopulator.PopulateRegistryAsync(cancellationToken);

        var descriptors = registry.ListAll().ToList();
        var response = new Response(descriptors);

        return response;
    }
}