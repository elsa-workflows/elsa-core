using System.Collections.Generic;

namespace Elsa.Abstractions.Multitenancy
{
    public class TenantConfiguration
    {
        private readonly IDictionary<string, string> _values = default!;

        public TenantConfiguration(IDictionary<string, string> values) => _values = values;

        public string this[string key] => _values[key];
    }
}
