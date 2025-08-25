using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Models;

/// <summary>
/// Represents a base configuration class for defining options specific to log sinks.
/// </summary>
public abstract record LogSinkOptions
{
    public LogLevel? MinLevel { get; init; }
    public Dictionary<string, LogLevel>? CategoryFilters { get; init; }
}