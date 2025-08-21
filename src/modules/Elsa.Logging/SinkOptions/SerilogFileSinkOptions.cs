namespace Elsa.Logging.SinkOptions;

public sealed record SerilogFileSinkOptions : SinkOptions
{
    public string Path { get; init; } = "";
    public string RollingInterval { get; init; } = "Day";
    public int? RetentionCount { get; init; }
    public string? Template { get; init; }
    public string? Formatter { get; init; } // "CompactJson" | null => text
}