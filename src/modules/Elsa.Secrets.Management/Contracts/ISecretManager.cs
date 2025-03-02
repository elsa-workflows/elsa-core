namespace Elsa.Secrets.Management;

public interface ISecretManager
{
    /// <summary>
    ///  Creates a new entity from the specified input.
    /// </summary>
    Task<Secret> CreateAsync(SecretInputModel input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new, updated version of the specified secret.
    /// </summary>
    Task<Secret> UpdateAsync(Secret entity, SecretInputModel input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the entity from the store.
    /// </summary>
    Task<Secret?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the entity from the store.
    /// </summary>
    Task<Secret?> FindAsync(SecretFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds all entities from the store matching the specified filter.
    /// </summary>
    Task<IEnumerable<Secret>> FindManyAsync(SecretFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities from the store.
    /// </summary>
    Task<IEnumerable<Secret>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entity from the store.
    /// </summary>
    Task DeleteAsync(Secret entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all entities from the store matching the specified filter.
    /// </summary>
    Task<long> DeleteManyAsync(SecretFilter filter, CancellationToken cancellationToken = default);
}