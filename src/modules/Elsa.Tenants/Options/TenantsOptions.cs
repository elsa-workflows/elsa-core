using Elsa.Common.Multitenancy;
using Elsa.Tenants.Providers;

namespace Elsa.Tenants.Options;

/// <summary>
/// Options for configuring the Tenants.
/// </summary>
public class TenantsOptions
{
    /// <summary>
    /// Gets or sets the tenants through configuration. Will be used by the <see cref="ConfigurationTenantsProvider"/>
    /// </summary>
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}