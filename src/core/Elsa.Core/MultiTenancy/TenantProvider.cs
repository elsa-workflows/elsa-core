using System;
using System.Linq;
using Elsa.Abstractions.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace Elsa.MultiTenancy
{
    public class TenantProvider : ITenantProvider
    {
        private Tenant? _currentTenant;

        public TenantProvider(IHttpContextAccessor accessor, ITenantStore tenantStore)
        {
            _currentTenant = GetTenant(accessor.HttpContext?.Request.Path, tenantStore);
        }

        public Tenant GetCurrentTenant()
        {
            if (_currentTenant == null) throw new Exception("Unable to retrieve current tenant");

            return _currentTenant;
        }

        public Tenant? TryGetCurrentTenant()
        {
            return _currentTenant;
        }

        public void SetCurrentTenant(Tenant? tenant)
        {
            _currentTenant = tenant;
        }

        private Tenant? GetTenant(PathString? path, ITenantStore tenantStore)
        {
            if (path == null) return null;

            var currentTenantPrefix = path?.Value.Split("/").FirstOrDefault(x => x != string.Empty);

            var tenantPrefixes = tenantStore.GetTenantPrefixes();

            if (tenantPrefixes.Contains(currentTenantPrefix))
            {
                return tenantStore.GetTenantByPrefix(currentTenantPrefix);
            }
            else
            {
                return null;
            }
        }
    }
}
