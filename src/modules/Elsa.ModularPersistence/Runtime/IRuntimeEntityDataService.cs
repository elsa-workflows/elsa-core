namespace Elsa.ModularPersistence.Runtime;

public interface IRuntimeEntityDataService
{
    ValueTask<RuntimeEntityRecord> CreateAsync(string definitionId, RuntimeEntitySaveRequest request, RuntimeStorageOperationContext context, string? providerName = null, CancellationToken cancellationToken = default);

    ValueTask<RuntimeEntityRecord?> GetAsync(string definitionId, string id, string? tenantId, RuntimeStorageOperationContext context, string? providerName = null, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> QueryAsync(string definitionId, RuntimeEntityQueryRequest request, RuntimeStorageOperationContext context, string? providerName = null, CancellationToken cancellationToken = default);

    ValueTask<RuntimeEntityRecord> UpdateAsync(string definitionId, RuntimeEntitySaveRequest request, RuntimeStorageOperationContext context, string? providerName = null, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(string definitionId, string id, string? tenantId, RuntimeStorageOperationContext context, string? providerName = null, long? expectedVersion = null, CancellationToken cancellationToken = default);
}
