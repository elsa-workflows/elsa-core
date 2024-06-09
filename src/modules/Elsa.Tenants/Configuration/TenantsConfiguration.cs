using Elsa.Tenants.Options;

namespace Elsa.Tenants.Configuration;

public class TenantsConfiguration
{
    public Action<MultitenancyOptions> MultitenancyOptions { get; set; } = options => { };
}