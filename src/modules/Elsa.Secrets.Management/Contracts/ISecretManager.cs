namespace Elsa.Secrets.Management;

public interface ISecretManager
{
    /// <summary>
    ///  Creates a new entity from the specified input.
    /// </summary>
    Task<Secret> CreateAsync(SecretInputModel input, CancellationToken cancellationToken = default);
    
    /// Creates a new, updated version of the specified secret.
    Task<Secret> UpdateAsync(Secret entity, SecretInputModel input, CancellationToken cancellationToken = default);
    
    /// Gets the entity from the store.
    Task<Secret?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Finds the entity from the store.
    Task<Secret?> FindAsync(SecretFilter filter, CancellationToken cancellationToken = default);
    
    /// Gets all entities from the store.
    Task<IEnumerable<Secret>> ListAsync(CancellationToken cancellationToken = default);

    /// Deletes the entity from the store.
    Task DeleteAsync(Secret entity, CancellationToken cancellationToken = default);

    /// Deletes all entities from the store matching the specified filter.
    Task<long> DeleteManyAsync(SecretFilter filter, CancellationToken cancellationToken = default);
}