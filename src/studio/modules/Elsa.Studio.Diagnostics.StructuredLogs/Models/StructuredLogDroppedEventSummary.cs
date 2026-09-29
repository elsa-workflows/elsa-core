namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Summary emitted when the backend drops structured log events under pressure.
/// </summary>
public class StructuredLogDroppedEventSummary
{
    public long DroppedCount { get; set; }
    public string? Reason { get; set; }
    public string? SourceId { get; set; }
}
