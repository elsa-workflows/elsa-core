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
}