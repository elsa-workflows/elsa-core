using Elsa.Common.Services;

namespace Elsa.Agents.Management;

public class MemoryServiceStore(MemoryStore<ServiceDefinition> memoryStore) : IServiceStore
{
    public Task SaveAsync(ServiceDefinition entity, CancellationToken cancellationToken = default)
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