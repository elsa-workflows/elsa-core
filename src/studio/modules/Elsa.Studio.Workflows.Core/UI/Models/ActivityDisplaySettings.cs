namespace Elsa.Studio.Workflows.UI.Models;

/// <summary>
/// Represents display settings for an activity.
/// </summary>
public class ActivityDisplaySettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityDisplaySettings"/> class.
    /// </summary>
    public ActivityDisplaySettings()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityDisplaySettings"/> class.
    /// </summary>
    public ActivityDisplaySettings(string color, string? icon = default)
    {
        Color = color;
        Icon = icon;
    }
    
    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    public string Color { get; set; } = default!;
}