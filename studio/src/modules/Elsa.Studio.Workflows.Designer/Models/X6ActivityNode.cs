using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the x6activity node.
/// </summary>
public class X6ActivityNode
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public string Id { get; set; } = null!;
    /// <summary>
    /// Gets or sets the shape.
    /// </summary>
    public string Shape { get; set; } = null!;
    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    public X6Position Position { get; set; } = null!;
    /// <summary>
    /// Gets or sets the size.
    /// </summary>
    public X6Size Size { get; set; } = null!;
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public JsonObject Data { get; set; } = null!;
    /// <summary>
    /// Gets or sets the ports.
    /// </summary>
    public X6Ports Ports { get; set; } = new();
    /// <summary>
    /// Gets or sets the activity stats.
    /// </summary>
    public ActivityStats? ActivityStats { get; set; }
    
}