using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Microsoft.Extensions.Configuration;

namespace Elsa.Multitenancy
{
    public class ConfigurationTenantStore : ITenantStore
    {
        private readonly IEnumerable<ITenant> _tenants;

        public ConfigurationTenantStore(IConfiguration configuration)
        {
            var tenants = new List<ITenant>();

            var multiTenancyEnabled = configuration.GetIsMultitenancyEnabled();

            if (multiTenancyEnabled)
            {
                var tenantsConfiguration = configuration.GetSection("Elsa:Multitenancy:Tenants").GetChildren();

                foreach (var tenantConfig in tenantsConfiguration)
                {
                    var name = tenantConfig.GetSection("Name").Value;
                    var id = name.ToLowerInvariant().Replace(" ", "-");

                    var properties = new Dictionary<string, object>(tenantConfig.AsEnumerable().Select(x => new KeyValuePair<string, object>(x.Key.Replace($"Elsa:Multitenancy:Tenants:{tenantConfig.Key}:", string.Empty), x.Value)));

                    tenants.Add(new Tenant(id, name, properties));
                }
            }
            else
            {
                var defaultPersistenceSection = configuration.GetSection($"Elsa:Features:DefaultPersistence");
                var connectionStringName = defaultPersistenceSection.GetValue<string>("ConnectionStringIdentifier");
                var dbConnectionString = defaultPersistenceSection.GetValue<string>("ConnectionString");

                if (string.IsNullOrWhiteSpace(dbConnectionString))
                {
                    dbConnectionString = configuration.GetConnectionString(connectionStringName);
                }

                var properties = new Dictionary<string, object>
                {
                    { "DatabaseConnectionString", dbConnectionString },
                    { "UrlPrefix", string.Empty },
                };

                tenants.Add(new Tenant("default", "Default", properties, isDefault: true));
            }

            _tenants = tenants;
        }

        public Task<IEnumerable<ITenant>> GetTenantsAsync() => Task.FromResult(_tenants);
    }
}
