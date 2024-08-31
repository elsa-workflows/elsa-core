using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;

namespace Elsa.Agents.Persistence.Contracts;

public interface IServiceStore
{
    /// Adds a new entity to the store.
    Task AddAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// Updates the entity to the store.
    Task UpdateAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// Gets the entity from the store.
    Task<ServiceDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Finds the entity from the store.
    Task<ServiceDefinition?> FindAsync(ServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// Gets all entities from the store.
    Task<IEnumerable<ServiceDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// Deletes the entity from the store.
    Task DeleteAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// Deletes all entities from the store that match the specified filter.
    Task<long> DeleteManyAsync(ServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
}