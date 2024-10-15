using JetBrains.Annotations;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// A tenant provider that provides the default tenant only. To turn on multitenancy, use another implementation.
/// </summary>
[UsedImplicitly]
public class DefaultTenantsProvider : ITenantsProvider
{
    private readonly Tenant[]  _tenants = { Tenant.Default };

    public ValueTask<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return new(_tenants);
    }

    public ValueTask<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var queryable = _tenants.AsQueryable();
        return new ValueTask<Tenant?>(filter.Apply(queryable).FirstOrDefault());
    }
}