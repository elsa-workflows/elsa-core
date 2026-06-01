using Elsa.ModularPersistence.Planning;

namespace Elsa.ModularPersistence.Runtime;

public interface IRuntimePhysicalizationOperations
{
    ValueTask<IReadOnlyCollection<StoragePhysicalizationPlan>> PlanAsync(string definitionId, string? providerName = null, CancellationToken cancellationToken = default);

    ValueTask<RuntimeStorageDefinition> PromoteIndexAsync(string definitionId, string indexName, RuntimeStorageOperationContext? context = null, CancellationToken cancellationToken = default);

    ValueTask<RuntimeStorageDefinition> DemoteIndexAsync(string definitionId, string indexName, RuntimeStorageOperationContext? context = null, CancellationToken cancellationToken = default);
}
