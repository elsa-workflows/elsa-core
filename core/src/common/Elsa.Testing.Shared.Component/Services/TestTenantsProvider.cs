using Elsa.Common.Multitenancy;
using JetBrains.Annotations;

namespace Elsa.Testing.Shared.Services;

[UsedImplicitly]
public class TestTenantsProvider(params string[] tenantIds) : ITenantsProvider
{
    public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenants = tenantIds.Select(id => new Tenant {Id = id, Name = id});
        return Task.FromResult(tenants);
    }

    public async Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var query = (await ListAsync(cancellationToken)).AsQueryable();
        return filter.Apply(query).FirstOrDefault();
    }
}