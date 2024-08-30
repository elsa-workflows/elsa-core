using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;

namespace Elsa.Agents.Persistence.Contracts;

public interface IAgentStore
{
    /// Adds a new entity to the store.
    Task AddAsync(AgentDefinition entity, CancellationToken cancellationToken = default);
    
    /// Updates the entity to the store.
    Task UpdateAsync(AgentDefinition entity, CancellationToken cancellationToken = default);
    
    /// Gets the entity from the store.
    Task<AgentDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Finds the entity from the store.
    Task<AgentDefinition?> FindAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// Gets all entities from the store.
    Task<IEnumerable<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// Deletes the entity from the store.
    Task DeleteAsync(AgentDefinition entity, CancellationToken cancellationToken = default);

    /// Deletes all entities from the store matching the specified filter.
    Task<long> DeleteManyAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default);
}