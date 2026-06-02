using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeEntities;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeEntities.Create;

internal sealed class Endpoint(IRuntimeEntityDataService dataService) : ElsaEndpoint<RuntimeEntityEndpointRequest, RuntimeEntityRecord>
{
    public override void Configure()
    {
        Post("/admin/modular-persistence/runtime-storage-definitions/{definitionId}/entities");
        ConfigurePermissions(ModularPersistencePermissions.WriteRuntimeEntities);
    }

    public override async Task<RuntimeEntityRecord> ExecuteAsync(RuntimeEntityEndpointRequest request, CancellationToken cancellationToken)
    {
        var saveRequest = new RuntimeEntitySaveRequest(request.Id, request.Data, request.TenantId, request.ExpectedVersion, request.Metadata);
        return await dataService.CreateAsync(Route<string>("definitionId")!, saveRequest, EndpointContext.Create(User), request.ProviderName, cancellationToken);
    }
}
