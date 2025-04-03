using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

public class DefaultTenantScopeFactory(ITenantAccessor tenantAccessor, IServiceScopeFactory serviceScopeFactory) : ITenantScopeFactory
{
    public TenantScope CreateScope(Tenant? tenant)
    {
        var serviceScope = serviceScopeFactory.CreateAsyncScope();
        return new(serviceScope, tenantAccessor, tenant);
    }
}