namespace Elsa.Agents.Management;

public interface IServiceStore
{
    /// Saves the entity to the store.
    Task SaveAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// Gets the entity from the store.
    Task<ServiceDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Gets all entities from the store.
    Task<IEnumerable<ServiceDefinition>> ListAsync(CancellationToken cancellationToken = default);
}