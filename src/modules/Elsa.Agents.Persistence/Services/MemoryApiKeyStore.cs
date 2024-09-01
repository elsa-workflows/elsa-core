using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.Common.Services;
using JetBrains.Annotations;

namespace Elsa.Agents.Persistence;

[UsedImplicitly]
public class MemoryApiKeyStore(MemoryStore<ApiKeyDefinition> memoryStore) : IApiKeyStore
{
    public Task AddAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Add(entity, x => x.Id);
        return Task.CompletedTask;
    }
    
    public Task UpdateAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Save(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<ApiKeyDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = memoryStore.Find(x => x.Id == id);
        return Task.FromResult(entity);
    }

    public Task<ApiKeyDefinition?> FindAsync(ApiKeyDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = memoryStore.Query(filter.Apply).FirstOrDefault();
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<ApiKeyDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = memoryStore.List();
        return Task.FromResult(entities);
    }

    public Task DeleteAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Delete(entity.Id);
        return Task.CompletedTask;
    }
    
    public Task<long> DeleteManyAsync(ApiKeyDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var agents = memoryStore.Query(filter.Apply).ToList();
        memoryStore.DeleteMany(agents, x => x.Id);
        return Task.FromResult<long>(agents.Count);
    }
}