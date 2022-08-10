using System.Collections.Generic;
using System.Linq;
using Autofac;
using Elsa.Extensions;
using Elsa.Multitenancy.Factories;
using Elsa.Multitenancy.Strategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Elsa.Multitenancy
{
    public static class TenantIdentificationStrategyFactory
    {
        public static IOverridableTenantIdentificationStrategy CreateSampleStrategy(out ITenant tenant)
        {
            tenant = TenantFactory.CreateDefaultTenant();
            return new PredefinedTenantIdentificationStrategy(tenant);
        }

        public static IOverridableTenantIdentificationStrategy CreateStrategy(IContainer container)
        {
            var configuration = container.Resolve<IConfiguration>();
            var multitenancyEnabled = configuration.GetIsMultitenancyEnabled();
            var tenantStore = container.Resolve<ITenantStore>();

            var tenants = tenantStore.GetTenantsAsync().GetAwaiter().GetResult();

            if (multitenancyEnabled)
            {
                var httpContextAccessor = container.Resolve<IHttpContextAccessor>();

                return new RequestTenantIdentificationStrategy(httpContextAccessor, tenantStore);
            }

            return new PredefinedTenantIdentificationStrategy(tenants.First(x => x.IsDefault));
        }
    }
}
