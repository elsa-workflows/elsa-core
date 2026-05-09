namespace Elsa.ServerLogs.Models;

public record RecentServerLogsResult(
    IReadOnlyCollection<ServerLogEvent> Items,
    long DroppedEvents);
