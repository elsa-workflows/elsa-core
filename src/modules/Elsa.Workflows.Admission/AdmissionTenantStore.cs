using Elsa.Common.Multitenancy;
using Elsa.Tenants;

namespace Elsa.Workflows.Admission;

internal sealed class AdmissionTenantStore(ITenantStore inner) : ITenantStore
{
    public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);
    public Task<Tenant?> FindAsync(string id, CancellationToken cancellationToken = default) => inner.FindAsync(id, cancellationToken);
    public Task<IEnumerable<Tenant>> FindManyAsync(TenantFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);
    public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default) => inner.ListAsync(cancellationToken);
    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) => throw Denied();
    public Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default) => throw Denied();
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw Denied();
    public Task<long> DeleteAsync(TenantFilter filter, CancellationToken cancellationToken = default) => throw Denied();
    private static InvalidOperationException Denied() => new("Tenant lifecycle mutation is unsupported without admission withdrawal coordination.");
}
