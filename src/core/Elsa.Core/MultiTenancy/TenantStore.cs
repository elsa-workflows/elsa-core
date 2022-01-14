using System.Collections.Generic;
using System.Linq;
using Elsa.Abstractions.MultiTenancy;
using Microsoft.Extensions.Configuration;

namespace Elsa.MultiTenancy
{
    public class TenantStore : ITenantStore
    {
        private List<Tenant> Tenants { get; }

        public TenantStore(IConfiguration configuration)
        {
            var tenants = new List<Tenant>();

            var tenantsConfiguration = configuration.GetSection("Elsa:Tenants").GetChildren();

            foreach (var tenantConfig in tenantsConfiguration)
            {
                var name = tenantConfig.GetSection("Name").Value;
                var prefix = tenantConfig.GetSection("Prefix").Value;
                var connectionString = tenantConfig.GetSection("ConnectionString").Value;

                tenants.Add(new Tenant(name, prefix, connectionString));
            }

            Tenants = tenants;
        }

        public Tenant GetTenantByPrefix(string prefix) => Tenants.FirstOrDefault(x => x.Prefix == prefix);

        public IList<Tenant> GetTenants() => Tenants;

        public IList<string> GetTenantPrefixes() => Tenants.Select(x => x.Prefix).ToList();
    }
}
