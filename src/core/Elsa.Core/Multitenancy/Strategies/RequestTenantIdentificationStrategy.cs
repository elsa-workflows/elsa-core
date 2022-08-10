using System.Collections.Generic;
using System.Linq;
using Elsa.Multitenancy.Extensions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Multitenancy
{
    public class RequestTenantIdentificationStrategy : IOverridableTenantIdentificationStrategy
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEnumerable<ITenant> _tenants;
        public ITenant? Tenant { get; set; }

        public RequestTenantIdentificationStrategy(IHttpContextAccessor httpContextAccessor, ITenantStore tenantStore)
        {
            _httpContextAccessor = httpContextAccessor;
            _tenants = tenantStore.GetTenantsAsync().GetAwaiter().GetResult();
        }

        public bool TryIdentifyTenant(out object tenantId)
        {
            tenantId = null;

            //check tenant property in case current tenant is overriden
            if (Tenant != null)
            {
                tenantId = Tenant.Id;
                return true;
            }


            var context = _httpContextAccessor.HttpContext;

            if (context?.Request != null)
            {
                // get very first segment  
                var urlPrefix = context.Request.Path.Value?.Split('/').FirstOrDefault(x => x != string.Empty);
                tenantId = _tenants.FirstOrDefault(x => x.GetTenantUrlPrefix() == urlPrefix)?.Id;
            }

            return tenantId != null;
        }
    }
}
