namespace Elsa.Studio.Workflows.Designer.Options;

/// <summary>
/// Represents the x6mousewheel.
/// </summary>
public class X6Mousewheel
{
    /// <summary>
    /// Indicates whether enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the factor.
    /// </summary>
    public double factor { get; set; } = 1.05;
    /// <summary>
    /// Gets or sets the min scale.
    /// </summary>
    public double MinScale { get; set; } = 0.4;
    /// <summary>
    /// Gets or sets the max scale.
    /// </summary>
    public double MaxScale { get; set; } = 3;
}