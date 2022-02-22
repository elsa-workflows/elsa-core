using System.Collections.Generic;

namespace Elsa.Abstractions.Multitenancy
{
    public class Tenant
    {
        public string Name { get; }
        public TenantConfiguration Configuration { get; }
        public bool IsDefault { get; }

        public Tenant(string name, Dictionary<string, string> configuration, bool isDefault = false)
        {
            Name = name;
            Configuration = new TenantConfiguration(configuration);
            IsDefault = isDefault;
        }
    }
}
