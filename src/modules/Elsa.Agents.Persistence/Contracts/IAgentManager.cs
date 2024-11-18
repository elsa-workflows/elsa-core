using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;

namespace Elsa.Agents.Persistence.Contracts;

public interface IAgentManager
{
    /// <summary>
    /// Adds a new entity to the store.
    /// </summary>
    Task AddAsync(AgentDefinition entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the entity to the store.
    /// </summary>
    Task UpdateAsync(AgentDefinition entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the entity from the store.
    /// </summary>
    Task<AgentDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the entity from the store.
    /// </summary>
    Task<AgentDefinition?> FindAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities from the store.
    /// </summary>
    Task<IEnumerable<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entity from the store.
    /// </summary>
    Task DeleteAsync(AgentDefinition entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all entities from the store matching the specified filter.
    /// </summary>
    Task<long> DeleteManyAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a unique name.
    /// </summary>
    Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a name is unique.
    /// </summary>
    Task<bool> IsNameUniqueAsync(string name, string? notId, CancellationToken cancellationToken = default);
}