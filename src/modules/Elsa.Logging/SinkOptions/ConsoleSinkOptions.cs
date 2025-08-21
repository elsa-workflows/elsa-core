using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Elsa.Logging.SinkOptions;

public sealed record ConsoleSinkOptions : SinkOptions
{
    // "Default" | "Simple" | "Systemd"
    public string Formatter { get; init; } = "Default";
    public string? TimestampFormat { get; init; }
    public bool? IncludeScopes { get; init; }

    // Default console
    public bool? DisableColors { get; init; }

    // Simple console
    public LoggerColorBehavior? ColorBehavior { get; init; }
    public bool? SingleLine { get; init; }
    public bool? UseUtcTimestamp { get; init; }
    public Dictionary<string, LogLevel>? CategoryFilters { get; init; }
}