namespace Elsa.Logging.Serilog;

public sealed record SerilogFileSinkOptions : SinkOptions.SinkOptions
{
    public string Path { get; init; } = "";
    public string RollingInterval { get; init; } = "Day";
    public int? RetentionCount { get; init; }
    public string? Template { get; init; }
    public string? Formatter { get; init; } // "CompactJson" | null => text
}