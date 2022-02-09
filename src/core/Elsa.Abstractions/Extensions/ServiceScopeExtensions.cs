using Elsa.Abstractions.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public static class ServiceScopeExtensions
    {
        public static void SetCurrentTenant(this IServiceScope scope, Tenant tenant)
        {
            var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
            tenantProvider.SetCurrentTenant(tenant);
        }
    }
}
