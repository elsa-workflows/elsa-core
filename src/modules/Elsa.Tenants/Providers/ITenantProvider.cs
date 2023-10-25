using Elsa.Tenants.Entities;
using Elsa.Tenants.Models;

namespace Elsa.Tenants.Providers;
public interface ITenantProvider
{
    List<Tenant> GetAllTenants();
    Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default);
}
