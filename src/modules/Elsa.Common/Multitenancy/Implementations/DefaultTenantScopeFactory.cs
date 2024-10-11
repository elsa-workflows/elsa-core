namespace Elsa.Common.Multitenancy;

public class DefaultTenantScopeFactory(ITenantAccessor tenantAccessor) : ITenantScopeFactory
{
    public TenantScope Create(Tenant? tenant)
    {
        return new TenantScope(tenantAccessor, tenant);
    }
}