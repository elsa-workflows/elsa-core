using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Microsoft.Extensions.Configuration;

namespace Elsa.Multitenancy
{
    public class TenantStore : ITenantStore
    {
        private readonly IList<ITenant> _tenants;

        public TenantStore(IConfiguration configuration)
        {
            var tenants = new List<ITenant>();

            var multiTenancyEnabled = configuration.GetValue<bool>("Elsa:Multitenancy:Enabled");

            if (multiTenancyEnabled)
            {
                var tenantsConfiguration = configuration.GetSection("Elsa:Multitenancy:Tenants").GetChildren();

                foreach (var tenantConfig in tenantsConfiguration)
                {
                    var name = tenantConfig.GetSection("Name").Value;

                    var configurationValues = new Dictionary<string, string>(tenantConfig.AsEnumerable().Select(x => new KeyValuePair<string, string>(x.Key.Replace($"Elsa:Multitenancy:Tenants:{tenantConfig.Key}:", string.Empty), x.Value)));

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

        public Task<IList<ITenant>> GetTenantsAsync() => Task.FromResult(_tenants);
    }
}
