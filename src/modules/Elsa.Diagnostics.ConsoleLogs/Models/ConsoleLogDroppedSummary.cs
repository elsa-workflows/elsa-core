namespace Elsa.Diagnostics.ConsoleLogs.Models;

public record ConsoleLogDroppedSummary(
    string? SourceId,
    ConsoleLogStream? Stream,
    string Reason,
    long Count,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);
