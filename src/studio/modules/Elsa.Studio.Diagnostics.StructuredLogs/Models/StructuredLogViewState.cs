namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Local page state for the structured log viewer.
/// </summary>
public class StructuredLogViewState
{
    public StructuredLogFilter Filter { get; set; } = new();
    public bool IsPaused { get; set; }
    public bool AutoScroll { get; set; } = true;
    public bool WrapMessages { get; set; } = true;
    public bool Compact { get; set; } = true;
    public StructuredLogConnectionStatus ConnectionStatus { get; set; } = StructuredLogConnectionStatus.Disconnected;
    public int VisibleRowCap { get; set; } = 1000;
    public int LocalDroppedRows { get; set; }
    public string? SelectedEventId { get; set; }
}
