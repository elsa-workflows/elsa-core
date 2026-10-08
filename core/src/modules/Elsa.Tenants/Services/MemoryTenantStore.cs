using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using JetBrains.Annotations;

namespace Elsa.Tenants;

[UsedImplicitly]
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

    public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = store.List();
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

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var found = store.Delete(id);
        return Task.FromResult(found);
    }

    public Task<long> DeleteAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var deletedCount = store.DeleteMany(filter.Apply(store.Queryable), GetId);
        return Task.FromResult(deletedCount);
    }
    
    private IQueryable<Tenant> Filter(IQueryable<Tenant> queryable, TenantFilter filter) => filter.Apply(queryable);

    private string GetId(Tenant tenant) => tenant.Id;
}