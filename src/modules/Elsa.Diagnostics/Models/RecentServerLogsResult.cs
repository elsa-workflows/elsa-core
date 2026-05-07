namespace Elsa.Diagnostics.Models;

public record RecentServerLogsResult(
    IReadOnlyCollection<ServerLogEvent> Items,
    long DroppedEvents);
