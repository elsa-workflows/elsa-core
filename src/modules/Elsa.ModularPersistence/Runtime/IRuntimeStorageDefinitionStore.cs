namespace Elsa.ModularPersistence.Runtime;

public interface IRuntimeStorageDefinitionStore
{
    ValueTask<RuntimeStorageDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<RuntimeStorageDefinition>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(RuntimeStorageDefinition definition, CancellationToken cancellationToken = default);
}
