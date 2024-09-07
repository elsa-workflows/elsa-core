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
    
    private static IQueryable<Secret> Filter(IQueryable<Secret> queryable, SecretFilter filter) => filter.Apply(queryable);
}