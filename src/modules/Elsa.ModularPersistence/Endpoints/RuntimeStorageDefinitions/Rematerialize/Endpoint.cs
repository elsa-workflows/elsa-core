using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions.Rematerialize;

internal sealed class Endpoint(IRuntimeStorageDefinitionManager manager) : ElsaEndpoint<PublishRuntimeStorageDefinitionRequest, RuntimeStorageDefinitionPublishResponse>
{
    public override void Configure()
    {
        Post("/admin/modular-persistence/runtime-storage-definitions/{id}/rematerialize");
        ConfigurePermissions(ModularPersistencePermissions.PublishRuntimeStorageDefinitions);
    }

    public override async Task<RuntimeStorageDefinitionPublishResponse> ExecuteAsync(PublishRuntimeStorageDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await manager.RematerializeAsync(Route<string>("id")!, request.ProviderName, EndpointContext.Create(User), cancellationToken);
        return new RuntimeStorageDefinitionPublishResponse(result.Definition, result.Succeeded, result.Errors.Select(x => $"{x.Code}: {x.Message}").ToArray());
    }
}
