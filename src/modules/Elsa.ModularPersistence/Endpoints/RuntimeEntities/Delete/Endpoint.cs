using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeEntities;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeEntities.Delete;

internal sealed class Endpoint(IRuntimeEntityDataService dataService) : ElsaEndpoint<DeleteRuntimeEntityRequest>
{
    public override void Configure()
    {
        Delete("/admin/modular-persistence/runtime-storage-definitions/{definitionId}/entities/{id}");
        ConfigurePermissions(ModularPersistencePermissions.DeleteRuntimeEntities);
    }

    public override async Task HandleAsync(DeleteRuntimeEntityRequest request, CancellationToken cancellationToken)
    {
        var deleted = await dataService.DeleteAsync(Route<string>("definitionId")!, Route<string>("id")!, Query<string?>("tenantId", false), EndpointContext.Create(User), request.ProviderName, request.ExpectedVersion, cancellationToken);
        if (deleted)
            await Send.NoContentAsync(cancellationToken);
        else
            await Send.NotFoundAsync(cancellationToken);
    }
}
