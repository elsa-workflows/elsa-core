namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the x6ports.
/// </summary>
public class X6Ports
{
    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public ICollection<X6Port> Items { get; set; } = new List<X6Port>();
}