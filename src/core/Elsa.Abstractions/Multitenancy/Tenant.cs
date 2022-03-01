using System.Collections.Generic;

namespace Elsa.Abstractions.Multitenancy
{
    public class Tenant : ITenant
    {
        public string Name { get; }
        public IDictionary<string, string> Configuration { get; }
        public bool IsDefault { get; }

        public Tenant(string name, IDictionary<string, string> configuration, bool isDefault = false)
        {
            Name = name;
            Configuration = configuration;
            IsDefault = isDefault;
        }
    }
}
