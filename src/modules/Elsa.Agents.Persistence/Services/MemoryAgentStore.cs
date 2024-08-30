using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.Common.Services;
using JetBrains.Annotations;

namespace Elsa.Agents.Persistence.Services;

[UsedImplicitly]
public class MemoryAgentStore(MemoryStore<AgentDefinition> memoryStore) : IAgentStore
{
    public Task AddAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Add(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Save(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<AgentDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = memoryStore.Find(x => x.Id == id);
        return Task.FromResult(entity);
    }

    public Task<AgentDefinition?> FindAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = memoryStore.Query(filter.Apply).FirstOrDefault();
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = memoryStore.List();
        return Task.FromResult(entities);
    }

    public Task DeleteAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Delete(entity.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteManyAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var agents = memoryStore.Query(filter.Apply).ToList();
        memoryStore.DeleteMany(agents, x => x.Id);
        return Task.FromResult<long>(agents.Count);
    }
}