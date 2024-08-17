namespace Elsa.Agents.Management;

public interface IApiKeyStore
{
    /// Saves the entity to the store.
    Task SaveAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default);
    
    /// Gets the entity from the store.
    Task<ApiKeyDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Gets all entities from the store.
    Task<IEnumerable<ApiKeyDefinition>> ListAsync(CancellationToken cancellationToken = default);
}