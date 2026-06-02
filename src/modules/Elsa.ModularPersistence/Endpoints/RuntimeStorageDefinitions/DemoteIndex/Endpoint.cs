using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions.DemoteIndex;

internal sealed class Endpoint(IRuntimePhysicalizationOperations operations) : ElsaEndpoint<RuntimeStorageIndexPhysicalizationRequest, RuntimeStorageDefinition>
{
    public override void Configure()
    {
        Post("/admin/modular-persistence/runtime-storage-definitions/{id}/indexes/demote");
        ConfigurePermissions(ModularPersistencePermissions.PublishRuntimeStorageDefinitions);
    }

    public override async Task<RuntimeStorageDefinition> ExecuteAsync(RuntimeStorageIndexPhysicalizationRequest request, CancellationToken cancellationToken)
    {
        return await operations.DemoteIndexAsync(Route<string>("id")!, request.IndexName, EndpointContext.Create(User), cancellationToken);
    }
}
