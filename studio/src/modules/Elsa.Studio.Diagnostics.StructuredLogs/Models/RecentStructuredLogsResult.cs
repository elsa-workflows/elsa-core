namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Recent log events returned by the backend.
/// </summary>
public class RecentStructuredLogsResult
{
    public ICollection<StructuredLogEvent> Items { get; set; } = new List<StructuredLogEvent>();
    public long DroppedEvents { get; set; }
}
