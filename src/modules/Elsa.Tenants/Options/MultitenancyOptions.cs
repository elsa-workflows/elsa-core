using Elsa.Common.Multitenancy;

namespace Elsa.Tenants.Options;

/// <summary>
/// Options for configuring the Tenants.
/// </summary>
public class MultitenancyOptions
{
    /// <summary>
    /// Gets or sets the tenant resolution pipeline builder.
    /// </summary>
    public ITenantResolverPipelineBuilder TenantResolverPipelineBuilder { get; set; } = new TenantResolverPipelineBuilder().Append<DefaultTenantResolver>();
}