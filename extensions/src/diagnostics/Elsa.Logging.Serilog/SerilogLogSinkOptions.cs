using Elsa.Logging.Models;

namespace Elsa.Logging.Serilog;

/// <summary>
/// Provides configuration options for a Serilog-based log sink.
/// </summary>
public sealed record SerilogLogSinkOptions : LogSinkOptions
{
    public string Path { get; init; } = "";
    public string RollingInterval { get; init; } = "Day";
    public int? RetentionCount { get; init; }
    public string? Template { get; init; }
    public string? Formatter { get; init; } // "CompactJson" | null => text
}