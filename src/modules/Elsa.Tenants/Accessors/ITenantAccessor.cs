using Elsa.Tenants.Entities;

namespace Elsa.Tenants.Accessors;
public interface ITenantAccessor
{
    public Task<Tenant?> GetCurrentTenantAsync();
}
