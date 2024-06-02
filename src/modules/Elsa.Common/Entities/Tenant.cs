using JetBrains.Annotations;

namespace Elsa.Common.Entities;

/// <summary>
/// Represents a tenant.
/// </summary>
[UsedImplicitly]
public class Tenant : Entity
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the configuration for a tenant.
    /// </summary>
    /// <remarks>
    /// The configuration can be used to store various settings and options specific to a tenant.
    /// </remarks>
    public IDictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the isolation mode for a tenant.
    /// </summary>
    public TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.Shared;
    
    public static readonly Tenant DefaultTenant = new()
    {
        Id = null!,
        Name = "Default"
    };
}