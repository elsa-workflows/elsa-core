using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.Secrets.Management;

[UsedImplicitly]
public class MemorySecretStore(MemoryStore<Secret> memoryStore) : ISecretStore
{
    public Task<Page<Secret>> FindManyAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = memoryStore.Query(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = memoryStore.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    public Task<Secret?> FindAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = memoryStore.Query(query => Filter(query, filter).OrderBy(order)).FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task AddAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Add(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Update(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<Secret?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = memoryStore.Find(x => x.Id == id);
        return Task.FromResult(result);
    }

    public Task<Secret?> FindAsync(SecretFilter filter, CancellationToken cancellationToken = default)
    {
        var result = memoryStore.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = memoryStore.List();
        return Task.FromResult(result);
    }

    public Task DeleteAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Delete(entity.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteManyAsync(SecretFilter filter, CancellationToken cancellationToken = default)
    {
        var agents = memoryStore.Query(filter.Apply).ToList();
        memoryStore.DeleteMany(agents, x => x.Id);
        return Task.FromResult<long>(agents.Count);
    }

    private IQueryable<Secret> Filter(IQueryable<Secret> queryable, SecretFilter filter) => filter.Apply(queryable);
}