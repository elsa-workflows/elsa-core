using Elsa.Workflows.Models;

namespace Elsa.Workflows.Contracts;

/// <summary>
/// Represents a sink for processing log entries.
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Gets the name of the log sink.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Determines if the sink is enabled for the specified log level and category.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="category">The log category.</param>
    /// <returns>True if enabled, false otherwise.</returns>
    bool IsEnabled(Models.LogLevel level, string category);
    
    /// <summary>
    /// Writes a log entry to the sink.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(ProcessLogEntry entry, CancellationToken cancellationToken = default);
}