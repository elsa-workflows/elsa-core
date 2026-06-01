using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions.Retire;

internal sealed class Endpoint(IRuntimeStorageDefinitionManager manager) : ElsaEndpointWithoutRequest<RuntimeStorageDefinition>
{
    public override void Configure()
    {
        Post("/admin/modular-persistence/runtime-storage-definitions/{id}/retire");
        ConfigurePermissions(ModularPersistencePermissions.DeleteRuntimeStorageDefinitions);
    }

    public override async Task<RuntimeStorageDefinition> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await manager.RetireAsync(Route<string>("id")!, EndpointContext.Create(User), cancellationToken);
    }
}
