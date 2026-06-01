using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions.Create;

internal sealed class Endpoint(IRuntimeStorageDefinitionManager manager) : ElsaEndpoint<SaveRuntimeStorageDefinitionRequest, RuntimeStorageDefinition>
{
    public override void Configure()
    {
        Post("/admin/modular-persistence/runtime-storage-definitions");
        ConfigurePermissions(ModularPersistencePermissions.WriteRuntimeStorageDefinitions);
    }

    public override async Task HandleAsync(SaveRuntimeStorageDefinitionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var draft = new RuntimeStorageDefinition(request.Id, request.SchemaName, request.StorageUnitName, request.Fields, request.Indexes, request.RequiredPermissions);
            var definition = await manager.SaveDraftAsync(draft, EndpointContext.Create(User), cancellationToken);
            await Send.OkAsync(definition, cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            AddError(e.Message);
            await Send.ErrorsAsync(cancellation: cancellationToken);
        }
    }
}
