using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Filters;
using Elsa.Connections.Persistence.Entities;

namespace Elsa.Connections.Persistence.Contracts;

public interface IConnectionStoreEx
{
    /// <summary>
    /// Get The Connection from the Store
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ConnectionDefinition> GetConnectionAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get All Connections from the store
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ICollection<ConnectionDefinition>> GetConnectionsAsync(CancellationToken cancellationToken = default);
    public Task<ICollection<ConnectionDefinition>> GetConnectionsFromTypeAsync(string type, CancellationToken cancellationToken = default);

    public Task AddConnectionConfigurationAsync(ConnectionDefinition model, CancellationToken cancellationToken = default);

    public Task UpdateConnectionAsync(string name, ConnectionDefinition model, CancellationToken cancellationToken = default);
    public Task DeleteConnectionConfigurationAsync(string id, CancellationToken cancellationToken = default);
}

public interface IConnectionStore
{
    /// <summary>
    /// Get The Connection from the Store
    /// </summary>
    public Task<ConnectionDefinition> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get All Connections from the store
    /// </summary>
    public Task<IEnumerable<ConnectionDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get All connection using Filter
    /// </summary>
    public Task<ConnectionDefinition> FindAsync(ConnectionDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get All connection using Filter
    /// </summary>
    public Task<IEnumerable<ConnectionDefinition>> FindManyAsync(ConnectionDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new entity to the store
    /// </summary>
    public Task AddAsync(ConnectionDefinition entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the entity to the store
    /// </summary>
    public Task UpdateAsync(ConnectionDefinition entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete the entity from the store
    /// </summary>
    public Task DeleteAsync(ConnectionDefinition entity, CancellationToken cancellationToken = default);
}
