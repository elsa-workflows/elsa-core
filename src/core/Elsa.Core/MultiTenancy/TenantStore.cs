using System.Collections.Generic;
using System.Linq;
using Elsa.Abstractions.Multitenancy;
using Microsoft.Extensions.Configuration;

namespace Elsa.Multitenancy
{
    public class TenantStore : ITenantStore
    {
        private readonly List<Tenant> _tenants;

        public TenantStore(IConfiguration configuration)
        {
            var tenants = new List<Tenant>();

            var multiTenancyEnabled = configuration.GetValue<bool>("Elsa:Multitenancy");

            if (multiTenancyEnabled)
            {
                var tenantsConfiguration = configuration.GetSection("Elsa:Tenants").GetChildren();

                foreach (var tenantConfig in tenantsConfiguration)
                {
                    var name = tenantConfig.GetSection("Name").Value;

                    var configurationValues = new Dictionary<string, string>(tenantConfig.AsEnumerable().Select(x => new KeyValuePair<string, string>(x.Key.Replace($"Elsa:Tenants:{tenantConfig.Key}:", string.Empty), x.Value)));

                    tenants.Add(new Tenant(name, configurationValues));
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

                var configurationValues = new Dictionary<string, string>
                {
                    { "DatabaseConnectionString", connectionString },
                    { "Prefix", string.Empty },
                };

                tenants.Add(new Tenant("Default", configurationValues, isDefault: true));
            }

            _tenants = tenants;
        }

        public IList<Tenant> GetTenants() => _tenants;
    }
}
