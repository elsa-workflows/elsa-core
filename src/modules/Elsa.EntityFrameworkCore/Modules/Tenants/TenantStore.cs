using Elsa.Common.Multitenancy;
using Elsa.Tenants;

namespace Elsa.EntityFrameworkCore.Modules.Tenants;

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

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(tenant, cancellationToken);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(tenant, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter.ById(id);
        await DeleteAsync(filter, cancellationToken);
    }

    public async Task DeleteAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        await store.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
    }
    
    private static IQueryable<Tenant> Filter(IQueryable<Tenant> query, TenantFilter filter) => filter.Apply(query);
}