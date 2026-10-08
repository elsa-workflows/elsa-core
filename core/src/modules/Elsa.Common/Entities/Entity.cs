namespace Elsa.Common.Entities;

/// <summary>
/// Represents an entity.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets or sets the ID of this entity.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID of the tenant that own this entity.
    /// </summary>
    public string? TenantId { get; set; }
}