using Microsoft.Extensions.Logging;

namespace Elsa.Logging.SinkOptions;

public abstract record SinkOptions
{
    public LogLevel? MinLevel { get; init; }
    public Dictionary<string, LogLevel>? CategoryFilters { get; init; }
}