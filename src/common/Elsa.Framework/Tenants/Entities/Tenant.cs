using System.Text.Json;
using Elsa.Framework.Entities;
using JetBrains.Annotations;

namespace Elsa.Framework.Tenants;

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
    /// Gets or sets the isolation mode for a tenant.
    /// </summary>
    public TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.Shared;

    /// <summary>
    /// Gets or sets the configuration for a tenant.
    /// </summary>
    /// <remarks>
    /// The configuration can be used to store various settings and options specific to a tenant.
    /// </remarks>
    public JsonElement? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the list of enabled features for the tenant.
    /// </summary>
    /// <remarks>
    /// The <see cref="EnabledFeatures"/> property represents a collection of features that are enabled for the tenant. Each feature is represented by a unique string identifier.
    /// </remarks>
    public ICollection<string> EnabledFeatures { get; set; } = new List<string>();
    
    public static readonly Tenant DefaultTenant = new()
    {
        Id = null!,
        Name = "Default"
    };
}