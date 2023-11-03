using Elsa.Tenants.Entities;

namespace Elsa.Tenants.Options;

/// <summary>
/// Options for configuring the Tenants.
/// </summary>
public class TenantsOptions
{
    /// <summary>
    /// Gets or sets the tenants.
    /// </summary>
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

    /// <summary>
    /// Gets or sets the custom claim type that hold the tenant id in the User's claims if tenantId is not in the User Store.
    /// If not set, <see cref="Microsoft.Identity.Web.ClaimConstants.TenantId" /> will be used
    /// </summary>
    public string? CustomTenantIdClaimsType { get; set; }
}