namespace Elsa.ServerLogs.Models;

public record ServerLogStreamItem(
    ServerLogEvent? LogEvent = null,
    ServerLogDroppedEventSummary? DroppedEvents = null)
{
    public static ServerLogStreamItem FromLogEvent(ServerLogEvent logEvent) => new(logEvent);

    public static ServerLogStreamItem FromDroppedEvents(ServerLogDroppedEventSummary summary) => new(DroppedEvents: summary);
}
