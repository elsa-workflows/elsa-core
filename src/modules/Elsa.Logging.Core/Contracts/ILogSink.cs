using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Contracts;

/// <summary>
/// Defines a contract for a log sink, responsible for handling log messages.
/// Implementers of this interface can capture, process, and store log records based on their specific logic or requirements.
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Gets the name of the log sink.
    /// This property uniquely identifies the log sink and is typically used for routing or retrieving specific sinks.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Writes a log entry asynchronously to a specific log sink.
    /// </summary>
    /// <param name="name">The name of the logger instance.</param>
    /// <param name="level">The severity level of the log entry.</param>
    /// <param name="message">The message to be logged.</param>
    /// <param name="arguments">Optional arguments related to the message.</param>
    /// <param name="properties">Optional additional log properties.</param>
    /// <param name="cancellationToken">Cancellation token to observe while waiting for the task to complete.</param>
    ValueTask WriteAsync(string name, LogLevel level, string message, object? arguments, IDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
}