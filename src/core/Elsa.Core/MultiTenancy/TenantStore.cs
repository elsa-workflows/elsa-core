using System.Collections.Generic;
using Elsa.Abstractions.MultiTenancy;
using Microsoft.Extensions.Configuration;

namespace Elsa.MultiTenancy
{
    public class TenantStore : ITenantStore
    {
        private readonly List<Tenant> _tenants;

        public TenantStore(IConfiguration configuration)
        {
            var tenants = new List<Tenant>();

            var multiTenancyEnabled = configuration.GetValue<bool>("Elsa:MultiTenancy");

            if (multiTenancyEnabled)
            {
                var tenantsConfiguration = configuration.GetSection("Elsa:Tenants").GetChildren();

                foreach (var tenantConfig in tenantsConfiguration)
                {
                    var name = tenantConfig.GetSection("Name").Value;
                    var prefix = tenantConfig.GetSection("Prefix").Value;
                    var connectionString = tenantConfig.GetSection("ConnectionString").Value;

                    tenants.Add(new Tenant(name, prefix, connectionString));
                }
            }
            else
            {
                var defaultPersistenceSection = configuration.GetSection($"Elsa:Features:DefaultPersistence");
                var connectionStringName = defaultPersistenceSection.GetValue<string>("ConnectionStringIdentifier");
                var connectionString = defaultPersistenceSection.GetValue<string>("ConnectionString");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = configuration.GetConnectionString(connectionStringName);
                }

                tenants.Add(new Tenant("Default", string.Empty, connectionString, isDefault: true));
            }

            _tenants = tenants;
        }

        public IList<Tenant> GetTenants() => _tenants;
    }
}
