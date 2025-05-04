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
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public IConfiguration Configuration { get; set; } = new ConfigurationBuilder().Build();
    
    public static readonly Tenant Default = new()
    {
        Id = null!,
        Name = "Default"
    };
}
