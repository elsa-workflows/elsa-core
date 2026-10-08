namespace Elsa.Diagnostics.StructuredLogs.Models;

public record StructuredLogDroppedEventSummary(
    string? SourceId,
    long DroppedCount,
    string Reason);
