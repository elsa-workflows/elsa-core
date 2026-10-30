namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the x6edge.
/// </summary>
public class X6Edge
{
    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public X6Endpoint Source { get; set; } = null!;
    /// <summary>
    /// Gets or sets the target.
    /// </summary>
    public X6Endpoint Target { get; set; } = null!;
    /// <summary>
    /// Gets or sets the shape.
    /// </summary>
    public string? Shape { get; set; }
    /// <summary>
    /// Gets or sets the vertices.
    /// </summary>
    public ICollection<X6Position> Vertices { get; set; } = [];
}