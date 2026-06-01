namespace Elsa.ModularPersistence.Runtime;

public interface IRuntimeStorageDefinitionManager
{
    ValueTask<RuntimeStorageDefinition> SaveDraftAsync(RuntimeStorageDefinition definition, CancellationToken cancellationToken = default);

    ValueTask<RuntimeStorageDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<RuntimeStorageDefinition>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<RuntimeStorageDefinitionPublishResult> PublishAsync(string id, string? providerName = null, CancellationToken cancellationToken = default);

    ValueTask<RuntimeStorageDefinitionPublishResult> RematerializeAsync(string id, string? providerName = null, CancellationToken cancellationToken = default);

    ValueTask<RuntimeStorageDefinition> RetireAsync(string id, CancellationToken cancellationToken = default);
}
