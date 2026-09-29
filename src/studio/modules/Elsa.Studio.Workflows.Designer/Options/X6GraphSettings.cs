namespace Elsa.Studio.Workflows.Designer.Options;

/// <summary>
/// Represents the x6graph settings.
/// </summary>
public class X6GraphSettings
{
    /// <summary>
    /// Gets or sets the grid.
    /// </summary>
    public X6Grid Grid { get; set; } = new();
    /// <summary>
    /// Gets or sets the magnet threshold.
    /// </summary>
    public double MagnetThreshold { get; set; }
    /// <summary>
    /// Gets or sets the panning.
    /// </summary>
    public X6Panning Panning { get; set; } = new();
    /// <summary>
    /// Gets or sets the mousewheel.
    /// </summary>
    public X6Mousewheel Mousewheel { get; set; } = new();
    public bool ResizingEnabled { get; set; } = true;
}