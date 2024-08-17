namespace Elsa.Agents.Management;

public interface IAgentStore
{
    /// Saves the entity to the store.
    Task SaveAsync(AgentDefinition entity, CancellationToken cancellationToken = default);
    
    /// Gets the entity from the store.
    Task<AgentDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Gets all entities from the store.
    Task<IEnumerable<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default);
}