using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeEntities.Get;

internal sealed class Endpoint(IRuntimeEntityDataService dataService) : ElsaEndpointWithoutRequest<RuntimeEntityRecord>
{
    public override void Configure()
    {
        Get("/admin/modular-persistence/runtime-storage-definitions/{definitionId}/entities/{id}");
        ConfigurePermissions(ModularPersistencePermissions.ReadRuntimeEntities);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var record = await dataService.GetAsync(Route<string>("definitionId")!, Route<string>("id")!, Query<string?>("tenantId", false), EndpointContext.Create(User), Query<string?>("providerName", false), cancellationToken);
        if (record == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(record, cancellationToken);
    }
}
