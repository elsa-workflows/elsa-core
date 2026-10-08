namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents an entity.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets or sets the ID of this entity.
    /// </summary>
    public string Id { get; set; } = default!;
}