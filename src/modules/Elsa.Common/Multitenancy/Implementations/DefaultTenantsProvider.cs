using JetBrains.Annotations;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// A tenant provider that provides the default tenant only. To turn on multitenancy, use another implementation.
/// </summary>
[UsedImplicitly]
public class DefaultTenantsProvider : ITenantsProvider
{
    private readonly Tenant[]  _tenants = { Tenant.Default };

    public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Tenant>>(_tenants);
    }

    public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var queryable = _tenants.AsQueryable();
        return Task.FromResult(filter.Apply(queryable).FirstOrDefault());
    }
}