namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the x6port.
/// </summary>
public class X6Port
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public string Id { get; set; } = null!;
    /// <summary>
    /// Gets or sets the group.
    /// </summary>
    public string Group { get; set; } = null!;
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string? Type { get; set; }
    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    public string? Position { get; set; }
    /// <summary>
    /// Gets or sets the attrs.
    /// </summary>
    public IDictionary<string, object>? Attrs { get; set; }
}