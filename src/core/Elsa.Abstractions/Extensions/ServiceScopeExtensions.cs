using Elsa.Abstractions.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public static class ServiceScopeExtensions
    {
        public static void SetCurrentTenant(this IServiceScope scope, ITenant tenant)
        {
            var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
            tenantProvider.SetCurrentTenantAsync(tenant);
        }
    }
}
