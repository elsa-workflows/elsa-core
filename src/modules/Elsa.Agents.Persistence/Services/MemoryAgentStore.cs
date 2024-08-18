using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Common.Services;

namespace Elsa.Agents.Persistence.Services;

public class MemoryAgentStore(MemoryStore<AgentDefinition> memoryStore) : IAgentStore
{
    public Task SaveAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Save(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<AgentDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = memoryStore.Find(x => x.Id == id);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = memoryStore.List();
        return Task.FromResult(entities);
    }
}