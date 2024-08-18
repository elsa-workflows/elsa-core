using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;

namespace Elsa.Agents.Persistence.Contracts;

public interface IApiKeyStore
{
    /// Adds a new entity to the store.
    Task AddAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default);
    
    /// Updates the entity to the store.
    Task UpdateAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default);
    
    /// Gets the entity from the store.
    Task<ApiKeyDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Finds a single entity using the specified filter.
    Task<ApiKeyDefinition?> FindAsync(ApiKeyDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// Gets all entities from the store.
    Task<IEnumerable<ApiKeyDefinition>> ListAsync(CancellationToken cancellationToken = default);
}