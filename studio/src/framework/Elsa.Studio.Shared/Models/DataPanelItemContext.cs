namespace Elsa.Studio.Models;

/// <summary>
/// Provides context for custom value renderers in data panel items.
/// </summary>
public class DataPanelItemContext
{
    /// <summary>
    /// Gets or sets the label of the data panel item.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the raw value of the data panel item.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the full data panel item for accessing all properties.
    /// </summary>
    public DataPanelItem Item { get; set; } = null!;
}
