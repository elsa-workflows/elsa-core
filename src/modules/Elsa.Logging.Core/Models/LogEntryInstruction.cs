using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Models;

/// <summary>
/// Represents the instruction for a log entry, including the details of the log message, its level, category, associated sinks, arguments, and additional attributes.
/// </summary>
public class LogEntryInstruction
{
    public ICollection<string> SinkNames { get; init; } = new List<string>();
    public string Category { get; init; } = null!;
    public LogLevel Level { get; init; } = LogLevel.Information;
    public string Message { get; init; } = string.Empty;
    public object? Arguments { get; init; }
    public IDictionary<string, object?> Attributes { get; init; } = new Dictionary<string, object?>();
}
