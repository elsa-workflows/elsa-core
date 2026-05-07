namespace Elsa.Diagnostics.Models;

public record ServerLogDroppedEventSummary(
    string? SourceId,
    long DroppedCount,
    string Reason);
