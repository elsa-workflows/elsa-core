using Elsa.Tenants.Constants;
using Elsa.Tenants.Contracts;
using Elsa.Tenants.Entities;
using Elsa.Tenants.Providers;
using Elsa.Tenants.Resolvers;
using Elsa.Tenants.Services;

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

    /// <summary>
    /// Gets or sets the claim type that hold the tenant id in the User's claims if tenantId is not in the User Store.
    /// If not set, <see cref="ClaimConstants.TenantId" /> will be used
    /// </summary>
    public string? TenantIdClaimsType { get; set; }

    /// <summary>
    /// Gets or sets the tenant resolution pipeline builder.
    /// </summary>
    public ITenantResolutionPipelineBuilder TenantResolutionPipelineBuilder { get; set; } = new TenantResolutionPipelineBuilder().Append<AmbientTenantResolver>();
}