using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;

namespace Elsa.Agents.Persistence.Contracts;

public interface IServiceStore
{
    /// <summary>
    /// Adds a new entity to the store.
    /// </summary>
    Task AddAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the entity to the store.
    /// </summary>
    Task UpdateAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the entity from the store.
    /// </summary>
    Task<ServiceDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the entity from the store.
    /// </summary>
    Task<ServiceDefinition?> FindAsync(ServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities from the store.
    /// </summary>
    Task<IEnumerable<ServiceDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entity from the store.
    /// </summary>
    Task DeleteAsync(ServiceDefinition entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all entities from the store that match the specified filter.
    /// </summary>
    Task<long> DeleteManyAsync(ServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
}