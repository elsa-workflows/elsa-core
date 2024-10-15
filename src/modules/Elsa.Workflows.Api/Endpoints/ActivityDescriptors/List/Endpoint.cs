using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

[PublicAPI]
internal class List : ElsaEndpointWithoutRequest<Response>
{
    private readonly IActivityRegistry _registry;
    private readonly IActivityRegistryPopulator _registryPopulator;
    private readonly ITenantAccessor _tenantAccessor;

    public List(IActivityRegistry registry, IActivityRegistryPopulator registryPopulator, ITenantAccessor tenantAccessor)
    {
        _registry = registry;
        _registryPopulator = registryPopulator;
        _tenantAccessor = tenantAccessor;
        var tenant = _tenantAccessor.CurrentTenant;
    }

    public override void Configure()
    {
        Get("/descriptors/activities");
        ConfigurePermissions("read:*", "read:activity-descriptors");
    }

    public override async Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var forceRefresh = Query<bool>("refresh", false);

        if (forceRefresh)
            await _registryPopulator.PopulateRegistryAsync(cancellationToken);

        var descriptors = _registry.ListAll().ToList();
        var response = new Response(descriptors);

        return response;
    }
}