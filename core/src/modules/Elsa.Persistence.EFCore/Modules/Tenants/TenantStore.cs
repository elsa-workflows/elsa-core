using Elsa.Common.Multitenancy;
using Elsa.Tenants;
using JetBrains.Annotations;

namespace Elsa.Persistence.EFCore.Modules.Tenants;

[UsedImplicitly]
public class EFCoreTenantStore(EntityStore<TenantsElsaDbContext, Tenant> store) : ITenantStore
{
    public async Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(query => Filter(query, filter), cancellationToken);
    }

    public async Task<Tenant?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter.ById(id);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<Tenant>> FindManyAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    public async Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await store.ListAsync(cancellationToken);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(tenant, cancellationToken);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(tenant, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter.ById(id);
        var count = await DeleteAsync(filter, cancellationToken);
        return count > 0;
    }

    public async Task<long> DeleteAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
    }
    
    private static IQueryable<Tenant> Filter(IQueryable<Tenant> query, TenantFilter filter) => filter.Apply(query);
}