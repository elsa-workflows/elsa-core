using Elsa.Common.Models;

namespace Elsa.Secrets.Management;

public interface ISecretStore
{
    /// Finds the entities from the store.
    Task<Page<Secret>> FindManyAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Secret>> FindManyAsync(SecretFilter filter, CancellationToken cancellationToken = default);
    
    /// Finds the entity from the store.
    Task<Secret?> FindAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// Adds a new entity to the store.
    Task AddAsync(Secret entity, CancellationToken cancellationToken = default);

    /// Updates the entity to the store.
    Task UpdateAsync(Secret entity, CancellationToken cancellationToken = default);

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