using System.Collections.Generic;

namespace Elsa.Abstractions.Multitenancy
{
    public interface ITenant
    {
        string Name { get; }
        IDictionary<string, string> Configuration { get; }
        bool IsDefault { get; }
    }
}
