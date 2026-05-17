namespace Elsa.Diagnostics.ConsoleLogs.Models;

public record ConsoleLogFilter
{
    public string? SourceId { get; init; }
    public ConsoleLogStream? Stream { get; init; }
    public string? Query { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? Limit { get; init; }
}
