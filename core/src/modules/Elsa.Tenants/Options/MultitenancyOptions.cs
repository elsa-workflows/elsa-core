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

    /// <summary>
    /// Gets or sets a value indicating whether a request, job or command whose tenant cannot be resolved is rejected instead of running as the default tenant.
    /// Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// When enabled, <see cref="DefaultTenantResolver"/> is no longer consulted as a fallback, so the tenant must be resolved by another resolver.
    /// Resolving the default tenant explicitly (an empty tenant ID) still succeeds.
    /// </remarks>
    public bool RejectUnresolvedTenants { get; set; }
}
