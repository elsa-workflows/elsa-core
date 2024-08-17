using Elsa.Common.Services;

namespace Elsa.Agents.Management;

public class MemoryApiKeyStore(MemoryStore<ApiKeyDefinition> memoryStore) : IApiKeyStore
{
    public Task SaveAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Save(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<ApiKeyDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = memoryStore.Find(x => x.Id == id);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<ApiKeyDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = memoryStore.List();
        return Task.FromResult(entities);
    }
}