using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Microsoft.AspNetCore.Http;

namespace Elsa.Multitenancy
{
    public class TenantProvider : ITenantProvider
    {
        private ITenant? _currentTenant;

        public TenantProvider(IHttpContextAccessor accessor, ITenantStore tenantStore)
        {
            _currentTenant = GetTenant(accessor.HttpContext?.Request.Path, tenantStore.GetTenantsAsync().GetAwaiter().GetResult());
        }

        public Task<ITenant> GetCurrentTenantAsync()
        {
            if (_currentTenant == null) throw new Exception("Unable to retrieve current tenant");

            return Task.FromResult(_currentTenant);
        }

        public Task<ITenant?> TryGetCurrentTenantAsync()
        {
            return Task.FromResult(_currentTenant);
        }

        public Task SetCurrentTenantAsync(ITenant tenant)
        {
            _currentTenant = tenant;
            return Task.CompletedTask;
        }

        private ITenant? GetTenant(PathString? path, IEnumerable<ITenant> tenants)
        {
            var defaultTenant = tenants.FirstOrDefault(x => x.IsDefault);

            if (defaultTenant != null) return defaultTenant;

            if (!path.HasValue) return null;            

            var currentTenantPrefix = path.Value.Value.Split("/").FirstOrDefault(x => x != string.Empty);

            return tenants.FirstOrDefault(x => x.GetPrefix() == currentTenantPrefix);
        }
    }
}
