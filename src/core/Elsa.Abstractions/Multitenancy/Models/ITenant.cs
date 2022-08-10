using System.Collections.Generic;

namespace Elsa.Multitenancy
{
    public interface ITenant
    {
        string Id { get; }
        string Name { get; }
        bool IsDefault { get; }
        IDictionary<string, object> Properties { get; }
    }
}
