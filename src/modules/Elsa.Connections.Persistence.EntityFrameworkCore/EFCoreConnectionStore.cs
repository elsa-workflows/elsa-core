using Elsa.Connections.Persistence.Contracts;
using Elsa.Connections.Persistence.Entities;
using Elsa.Connections.Persistence.Filters;
using Elsa.EntityFrameworkCore;
using JetBrains.Annotations;

namespace Elsa.Connections.Persistence.EntityFrameworkCore;

/// <summary>
/// An EF Core implementation of <see cref="IConnectionStore"/>.
/// </summary>
[UsedImplicitly]
public class EFCoreConnectionStore(EntityStore<ConnectionDbContext, ConnectionDefinition> store) : IConnectionStore
{
    public Task AddAsync(ConnectionDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(ConnectionDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.UpdateAsync(entity, cancellationToken);
    }

    public Task<ConnectionDefinition> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new ConnectionDefinitionFilter
        {
            Id = id
        };

        return FindAsync(filter, cancellationToken);
    }

    public Task<ConnectionDefinition> FindAsync(ConnectionDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter.Apply, cancellationToken);
    }

    public Task<IEnumerable<ConnectionDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        return store.ListAsync(cancellationToken);
    }

    public Task DeleteAsync(ConnectionDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.DeleteAsync(entity, cancellationToken);
    }

    public Task<IEnumerable<ConnectionDefinition>> FindManyAsync(ConnectionDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return store.QueryAsync(filter.Apply, cancellationToken);
    }
}