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

    private IQueryable<Secret> Filter(IQueryable<Secret> queryable, SecretFilter filter) => filter.Apply(queryable);
}