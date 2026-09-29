namespace Elsa.Studio.Workflows.Designer.Options;

/// <summary>
/// Represents the x6grid.
/// </summary>
public class X6Grid
{
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string Type { get; set; } = "dot";
    /// <summary>
    /// Indicates whether visible.
    /// </summary>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets the size.
    /// </summary>
    public double Size { get; set; } = 20;
    /// <summary>
    /// Gets or sets the args.
    /// </summary>
    public object? Args { get; set; } = new X6GridArgs();
}