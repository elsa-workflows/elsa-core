using System.Text.Json.Serialization;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the x6graph.
/// </summary>
public class X6Graph
{
    [JsonConstructor]
    public X6Graph()
    {
    }
    
    public X6Graph(IEnumerable<X6ActivityNode> nodes, IEnumerable<X6Edge> edges)
    {
        Nodes = nodes.ToList();
        Edges = edges.ToList();
    }

    /// <summary>
    /// Gets or sets the nodes.
    /// </summary>
    public ICollection<X6ActivityNode> Nodes { get; set; } = new List<X6ActivityNode>();
    /// <summary>
    /// Gets or sets the edges.
    /// </summary>
    public ICollection<X6Edge> Edges { get; set; } = new List<X6Edge>();

    /// <summary>
    /// Gets or sets the layout orientation for constrained graph designers.
    /// </summary>
    public string? LayoutOrientation { get; set; }
}
