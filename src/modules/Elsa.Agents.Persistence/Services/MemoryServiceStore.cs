using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Common.Services;
using JetBrains.Annotations;

namespace Elsa.Agents.Persistence.Services;

[UsedImplicitly]
public class MemoryServiceStore(MemoryStore<ServiceDefinition> memoryStore) : IServiceStore
{
    public Task AddAsync(ServiceDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Add(entity, x => x.Id);
        return Task.CompletedTask;
    }
    
    public Task UpdateAsync(ServiceDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Save(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<ServiceDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = memoryStore.Find(x => x.Id == id);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<ServiceDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = memoryStore.List();
        return Task.FromResult(entities);
    }
}