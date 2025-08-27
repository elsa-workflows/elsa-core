using Elsa.Logging.Models;
using Microsoft.Extensions.Logging.Console;

namespace Elsa.Logging.Console;

/// <summary>
/// Represents configuration options for a console-based log sink.
/// </summary>
/// <remarks>
/// Provides properties to configure the appearance and behavior of log output when using a console sink.
/// Includes support for various log formatter types, timestamp customization, and scope inclusion settings.
/// </remarks>
public sealed record ConsoleLogSinkOptions : LogSinkOptions
{
    // "Default" | "Simple" | "Systemd"
    public string Formatter { get; init; } = "Default";
    public string? TimestampFormat { get; init; }
    public bool? IncludeScopes { get; init; } = true;

    // Default console
    public bool? DisableColors { get; init; }

    // Simple console
    public LoggerColorBehavior? ColorBehavior { get; init; }
    public bool? SingleLine { get; init; }
    public bool? UseUtcTimestamp { get; init; }
    public bool JsonIndented { get; set; } = true;
}