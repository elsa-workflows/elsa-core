using Elsa.Common.Models;

namespace Elsa.Secrets.Management;

public interface ISecretStore
{
    /// <summary>
    /// Finds the entities from the store.
    /// </summary>
    Task<Page<Secret>> FindManyAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Secret>> FindManyAsync(SecretFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the entity from the store.
    /// </summary>
    Task<Secret?> FindAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the store.
    /// </summary>
    Task AddAsync(Secret entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the entity to the store.
    /// </summary>
    Task UpdateAsync(Secret entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the entity from the store.
    /// </summary>
    Task<Secret?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the entity from the store.
    /// </summary>
    Task<Secret?> FindAsync(SecretFilter filter, CancellationToken cancellationToken = default);

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