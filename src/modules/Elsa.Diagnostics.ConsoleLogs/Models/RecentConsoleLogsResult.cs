namespace Elsa.Diagnostics.ConsoleLogs.Models;

public record RecentConsoleLogsResult(
    IReadOnlyCollection<ConsoleLogLine> Items,
    IReadOnlyCollection<ConsoleLogDroppedSummary>? Dropped = null,
    IReadOnlyCollection<ConsoleLogSource>? Sources = null);
