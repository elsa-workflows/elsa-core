using Elsa.Abstractions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions.List;

internal sealed class Endpoint(IRuntimeStorageDefinitionManager manager) : ElsaEndpointWithoutRequest<RuntimeStorageDefinitionsResponse>
{
    public override void Configure()
    {
        Get("/admin/modular-persistence/runtime-storage-definitions");
        ConfigurePermissions(ModularPersistencePermissions.ReadRuntimeStorageDefinitions);
    }

    public override async Task<RuntimeStorageDefinitionsResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var definitions = await manager.ListAsync(cancellationToken);
        return new RuntimeStorageDefinitionsResponse(definitions);
    }
}
