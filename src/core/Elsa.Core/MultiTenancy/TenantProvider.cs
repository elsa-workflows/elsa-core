using System;
using System.Collections.Generic;
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
            _currentTenant = GetTenant(accessor.HttpContext?.Request.Path, tenantStore.GetTenants());
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

        public void SetCurrentTenant(Tenant tenant)
        {
            _currentTenant = tenant;
        }

        private Tenant? GetTenant(PathString? path, IEnumerable<Tenant> tenants)
        {
            var defaultTenant = tenants.FirstOrDefault(x => x.IsDefault);

            if (defaultTenant != null) return defaultTenant;

            if (!path.HasValue) return null;            

            var currentTenantPrefix = path.Value.Value.Split("/").FirstOrDefault(x => x != string.Empty);

            return tenants.FirstOrDefault(x => x.Prefix == currentTenantPrefix);
        }
    }
}
