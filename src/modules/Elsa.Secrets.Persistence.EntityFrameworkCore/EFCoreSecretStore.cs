using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Secrets.Management;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore;

/// An EF Core implementation of <see cref="ISecretStore"/>.
[UsedImplicitly]
public class EFCoreSecretStore(EntityStore<SecretsDbContext, Secret> store) : ISecretStore
{
    public async Task<Page<Secret>> FindManyAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(query => Filter(query, filter), cancellationToken).LongCount();
        var secrets = await store.QueryAsync(query =>  Filter(query, filter).Paginate(pageArgs).OrderBy(order), cancellationToken).ToList();
        return new(secrets, count);
    }

    public async Task<Secret?> FindAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(query => Filter(query, filter).OrderBy(order), cancellationToken).FirstOrDefault();
    }

    public async Task AddAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(entity, cancellationToken);
    }

    public async Task<Secret?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new SecretFilter
        {
            Id = id
        };
        return await FindAsync(filter, cancellationToken);
    }

    public Task<Secret?> FindAsync(SecretFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter.Apply, cancellationToken);
    }

    public async Task<IEnumerable<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await store.ListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        await store.DeleteAsync(entity, cancellationToken);
    }

    public async Task<long> DeleteManyAsync(SecretFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }

    private static IQueryable<Secret> Filter(IQueryable<Secret> queryable, SecretFilter filter) => filter.Apply(queryable);
}