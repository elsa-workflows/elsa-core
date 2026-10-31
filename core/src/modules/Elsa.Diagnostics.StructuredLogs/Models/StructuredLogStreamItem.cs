namespace Elsa.Diagnostics.StructuredLogs.Models;

public record StructuredLogStreamItem(
    StructuredLogEvent? LogEvent = null,
    StructuredLogDroppedEventSummary? DroppedEvents = null)
{
    public static StructuredLogStreamItem FromLogEvent(StructuredLogEvent logEvent) => new(logEvent);

    public static StructuredLogStreamItem FromDroppedEvents(StructuredLogDroppedEventSummary summary) => new(DroppedEvents: summary);
}
