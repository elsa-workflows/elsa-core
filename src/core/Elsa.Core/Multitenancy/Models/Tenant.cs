using System.Collections.Generic;

namespace Elsa.Multitenancy
{
    public class Tenant : ITenant
    {
        public Tenant(string id, string name, IDictionary<string, object> properties, bool isDefault = false)
        {
            Id = id;
            Name = name;
            Properties = properties;
            IsDefault = isDefault;
        }

        public string Id { get; }
        public string Name { get; }
        public bool IsDefault { get; }
        public IDictionary<string, object> Properties { get; }
    }
}
