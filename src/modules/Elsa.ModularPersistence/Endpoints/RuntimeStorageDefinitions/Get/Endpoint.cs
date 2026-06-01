using Elsa.Abstractions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions.Get;

internal sealed class Endpoint(IRuntimeStorageDefinitionManager manager) : ElsaEndpointWithoutRequest<RuntimeStorageDefinition>
{
    public override void Configure()
    {
        Get("/admin/modular-persistence/runtime-storage-definitions/{id}");
        ConfigurePermissions(ModularPersistencePermissions.ReadRuntimeStorageDefinitions);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var definition = await manager.GetAsync(Route<string>("id")!, cancellationToken);
        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(definition, cancellationToken);
    }
}
