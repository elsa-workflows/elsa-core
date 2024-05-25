using Elsa.Common.Entities;
using Elsa.Tenants.Providers;
using Elsa.Tenants.Resolvers;
using Elsa.Tenants.Services;

namespace Elsa.Tenants.Options;

/// <summary>
/// Options for configuring the Tenants.
/// </summary>
public class MultitenancyOptions
{
    /// <summary>
    /// Gets or sets the tenants through configuration. Will be used by the <see cref="ConfigurationTenantsProvider"/>
    /// </summary>
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

    /// <summary>
    /// Gets or sets the tenant resolution pipeline builder.
    /// </summary>
    public ITenantResolutionPipelineBuilder TenantResolutionPipelineBuilder { get; set; } = new TenantResolutionPipelineBuilder().Append<AmbientTenantResolver>();
}