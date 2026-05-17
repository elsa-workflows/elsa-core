namespace Elsa.Diagnostics.ConsoleLogs.Models;

public record ConsoleLogStreamItem(
    ConsoleLogLine? Line = null,
    ConsoleLogDroppedSummary? DroppedLines = null,
    ConsoleLogSource? Source = null)
{
    public static ConsoleLogStreamItem FromLine(ConsoleLogLine line) => new(Line: line);

    public static ConsoleLogStreamItem FromDroppedLines(ConsoleLogDroppedSummary summary) => new(DroppedLines: summary);

    public static ConsoleLogStreamItem FromSource(ConsoleLogSource source) => new(Source: source);
}
