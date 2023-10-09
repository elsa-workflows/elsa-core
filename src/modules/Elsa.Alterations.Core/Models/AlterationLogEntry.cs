using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Models;

/// <summary>
/// A log entry for an alteration.
/// </summary>
/// <param name="Message">The log message.</param>
/// <param name="LogLevel">The log level.</param>
/// <param name="Timestamp">The timestamp when the log entry was created.</param>
public record AlterationLogEntry(string Message, LogLevel LogLevel, DateTimeOffset Timestamp);