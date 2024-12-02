using Elsa.Common.Multitenancy;
using Elsa.Common.Services;

namespace Elsa.Tenants;

public class MemoryTenantStore(MemoryStore<Tenant> store) : ITenantStore
{
    public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task<Tenant?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter.ById(id);
        return FindAsync(filter, cancellationToken);
    }

    public Task<IEnumerable<Tenant>> FindManyAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        store.Add(tenant, GetId);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        store.Update(tenant, GetId);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        store.Delete(id);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        store.DeleteMany(filter.Apply(store.Queryable), GetId);
        return Task.CompletedTask;
    }
    
    private IQueryable<Tenant> Filter(IQueryable<Tenant> queryable, TenantFilter filter) => filter.Apply(queryable);

    private string GetId(Tenant tenant) => tenant.Id;
}