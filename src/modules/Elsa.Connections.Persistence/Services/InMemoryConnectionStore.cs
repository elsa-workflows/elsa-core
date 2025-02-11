using Elsa.Common.Services;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Connections.Persistence.Filters;
using Elsa.Connections.Persistence.Entities;

namespace Elsa.Connections.Persistence.Services;

public class InMemoryConnectionStore(MemoryStore<ConnectionDefinition> memoryStore) : IConnectionStore
{
    public Task AddAsync(ConnectionDefinition model, CancellationToken cancellationToken = default)
    {
        memoryStore.Add(model, model => model.Id);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ConnectionDefinition model, CancellationToken cancellationToken = default)
    {
        memoryStore.Delete(model.Id);
        return Task.CompletedTask;
    }

    public Task<ConnectionDefinition> GetAsync(string Id, CancellationToken cancellationToken = default)
    {
        var result = memoryStore.Find(c => c.Id == Id);
        return Task.FromResult(result);
    }

    public Task<IEnumerable<ConnectionDefinition>> ListAsync(CancellationToken cancellationToken)
    {
        var results = memoryStore.List();
        return Task.FromResult(results);
    }
    public Task<ConnectionDefinition> FindAsync(ConnectionDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = memoryStore.Query(filter.Apply).FirstOrDefault();
        return Task.FromResult(entities);
    }

    public Task<IEnumerable<ConnectionDefinition>> FindManyAsync(ConnectionDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = memoryStore.Query(filter.Apply);
        return Task.FromResult(entities);
    }

    public Task UpdateAsync(ConnectionDefinition model, CancellationToken cancellationToken = default)
    {
        memoryStore.Update(model, model => model.Id);
        return Task.CompletedTask;
    }
}
