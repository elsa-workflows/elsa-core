using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.EntityFrameworkCore.Common;
using JetBrains.Annotations;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

/// An EF Core implementation of <see cref="IServiceStore"/>.
[UsedImplicitly]
public class EFCoreAgentStore(EntityStore<AgentsDbContext, AgentDefinition> store) : IAgentStore
{
    public Task AddAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.AddAsync(entity, cancellationToken);
    }
    
    public Task UpdateAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.UpdateAsync(entity, cancellationToken);
    }

    public Task<AgentDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new AgentDefinitionFilter
        {
            Id = id
        };
        
        return FindAsync(filter, cancellationToken);
    }

    public Task<AgentDefinition?> FindAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter.Apply, cancellationToken);
    }

    public Task<IEnumerable<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        return store.ListAsync(cancellationToken);
    }

    public Task DeleteAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.DeleteAsync(entity, cancellationToken);
    }
}