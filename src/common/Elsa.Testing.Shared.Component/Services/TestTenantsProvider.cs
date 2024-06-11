using Elsa.Framework.Tenants;

namespace Elsa.Testing.Shared.Services;

public class TestTenantsProvider(params string[] tenantIds) : ITenantsProvider
{
    public ValueTask<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenants = tenantIds.Select(id => new Tenant {Id = id, Name = id});
        return new(tenants);
    }

    public async ValueTask<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var query = (await ListAsync(cancellationToken)).AsQueryable();
        return filter.Apply(query).FirstOrDefault();
    }
}