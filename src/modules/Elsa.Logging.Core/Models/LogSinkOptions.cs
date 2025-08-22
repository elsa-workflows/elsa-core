using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Models;

public abstract record LogSinkOptions
{
    public LogLevel? MinLevel { get; init; }
    public Dictionary<string, LogLevel>? CategoryFilters { get; init; }
}