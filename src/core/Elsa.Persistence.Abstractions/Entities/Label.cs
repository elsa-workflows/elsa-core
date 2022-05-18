namespace Elsa.Persistence.Entities;

/// <summary>
/// Represents an individual label.
/// </summary>
public class Label : Entity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Color { get; set; }
}