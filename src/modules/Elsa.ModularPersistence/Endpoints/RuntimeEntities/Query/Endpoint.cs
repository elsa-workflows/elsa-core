using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeEntities;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeEntities.Query;

internal sealed class Endpoint(IRuntimeEntityDataService dataService) : ElsaEndpoint<RuntimeEntityQueryEndpointRequest, RuntimeEntityRecordsResponse>
{
    public override void Configure()
    {
        Post("/admin/modular-persistence/runtime-storage-definitions/{definitionId}/entities/query");
        ConfigurePermissions(ModularPersistencePermissions.ReadRuntimeEntities);
    }

    public override async Task<RuntimeEntityRecordsResponse> ExecuteAsync(RuntimeEntityQueryEndpointRequest request, CancellationToken cancellationToken)
    {
        var query = new RuntimeEntityQueryRequest(request.Filters, limit: request.Limit, offset: request.Offset, tenantId: request.TenantId);
        var records = await dataService.QueryAsync(Route<string>("definitionId")!, query, EndpointContext.Create(User), request.ProviderName, cancellationToken);
        return new RuntimeEntityRecordsResponse(records);
    }
}
