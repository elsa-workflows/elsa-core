namespace Elsa.ServerLogs.Models;

public record ServerLogDroppedEventSummary(
    string? SourceId,
    long DroppedCount,
    string Reason);
