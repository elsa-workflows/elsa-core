namespace Elsa.Diagnostics.StructuredLogs.Models;

public record RecentStructuredLogsResult(
    IReadOnlyCollection<StructuredLogEvent> Items,
    long DroppedEvents);
