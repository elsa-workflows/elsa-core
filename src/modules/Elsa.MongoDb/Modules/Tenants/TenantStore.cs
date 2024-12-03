using Elsa.Common.Multitenancy;
using Elsa.Identity.Contracts;
using Elsa.MongoDb.Common;
using Elsa.Tenants;
using JetBrains.Annotations;

namespace Elsa.MongoDb.Modules.Tenants;

/// <summary>
/// A MongoDb implementation of <see cref="IRoleStore"/>.
/// </summary>
[UsedImplicitly]
public class MongoTenantStore(MongoDbStore<Tenant> store) : ITenantStore
{
    /// <inheritdoc />
    public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(query => Filter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Tenant?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter.ById(id);
        return FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Tenant>> FindManyAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    public async Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await store.ListAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        return store.AddAsync(tenant, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        return store.SaveAsync(tenant, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter.ById(id);
        var count = await DeleteAsync(filter, cancellationToken);
        return count > 0;
    }

    /// <inheritdoc />
    public Task<long> DeleteAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        return store.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);
    }

    private static IQueryable<Tenant> Filter(IQueryable<Tenant> query, TenantFilter filter)
    {
        return filter.Apply(query);
    }
}