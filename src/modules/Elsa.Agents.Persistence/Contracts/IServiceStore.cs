using Elsa.Agents.Persistence.Entities;

namespace Elsa.Agents.Persistence.Contracts;

public interface IServiceStore
{
    /// Adds a new entity to the store.
    Task AddAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// Updates the entity to the store.
    Task UpdateAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// Gets the entity from the store.
    Task<ServiceDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Gets all entities from the store.
    Task<IEnumerable<ServiceDefinition>> ListAsync(CancellationToken cancellationToken = default);
}