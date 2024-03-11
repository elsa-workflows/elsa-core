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
}
