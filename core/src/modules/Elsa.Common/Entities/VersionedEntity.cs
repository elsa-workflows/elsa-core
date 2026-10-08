namespace Elsa.Common.Entities;

/// <summary>
/// Represents an entity that is versioned.
/// </summary>
public abstract class VersionedEntity : Entity
{
    /// <summary>
    /// Gets or sets the date and time this entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the version of this entity.
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets whether this entity is the latest version.
    /// </summary>
    public bool IsLatest { get; set; }
    
    /// <summary>
    /// Gets or sets whether this entity is published.
    /// </summary>
    public bool IsPublished { get; set; }
}