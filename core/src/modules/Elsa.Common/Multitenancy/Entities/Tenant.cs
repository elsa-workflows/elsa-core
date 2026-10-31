using Elsa.Common.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents a tenant.
/// </summary>
[UsedImplicitly]
public class Tenant : Entity
{
    /// <summary>
    /// The ID used for the default tenant.
    /// </summary>
    public const string DefaultTenantId = "";

    /// <summary>
    /// The ID used for tenant-agnostic entities that are available to all tenants.
    /// </summary>
    public const string AgnosticTenantId = "*";

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public IConfiguration Configuration { get; set; } = new ConfigurationBuilder().Build();

    public static readonly Tenant Default = new()
    {
        Id = DefaultTenantId,
        Name = "Default"
    };
}
