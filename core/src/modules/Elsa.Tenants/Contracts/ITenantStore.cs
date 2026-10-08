using Elsa.Common.Multitenancy;

namespace Elsa.Tenants;

public interface ITenantStore
{
    Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default);
    Task<Tenant?> FindAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> FindManyAsync(TenantFilter filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<long> DeleteAsync(TenantFilter filter, CancellationToken cancellationToken = default);
}