using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;

namespace Elsa.Agents.Persistence.Contracts;

public interface IApiKeyStore
{
    /// <summary>
    /// Adds a new entity to the store.
    /// </summary>
    Task AddAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the entity to the store.
    /// </summary>
    Task UpdateAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the entity from the store.
    /// </summary>
    Task<ApiKeyDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a single entity using the specified filter.
    /// </summary>
    Task<ApiKeyDefinition?> FindAsync(ApiKeyDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities from the store.
    /// </summary>
    Task<IEnumerable<ApiKeyDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entity from the store.
    /// </summary>
    Task DeleteAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all entities from the store that match the specified filter.
    /// </summary>
    Task<long> DeleteManyAsync(ApiKeyDefinitionFilter filter, CancellationToken cancellationToken = default);
}