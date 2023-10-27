using Elsa.Tenants.Entities;

namespace Elsa.Tenants.Accessors;
public interface ITenantAccessor
{
    Task<Tenant?> GetCurrentTenantAsync();
    Task<string?> GetCurrentTenantIdAsync();
}
