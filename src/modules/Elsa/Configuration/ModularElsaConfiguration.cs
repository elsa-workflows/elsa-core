using Elsa.Tenants.Options;

namespace Elsa.Configuration;

public class ModularElsaConfiguration
{
    public Action<MultitenancyOptions> Multitenancy { get; set; } = options => { };
}