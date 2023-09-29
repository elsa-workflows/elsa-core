using Elsa.Alterations.Core.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// A log of alterations that have been applied.
/// </summary>
public interface IAlterationLog
{
    /// <summary>
    /// Appends a log entry to the log.
    /// </summary>
    /// <param name="message">A message to log.</param>
    /// <param name="logLevel">The log level.</param>
    void Append(string message, LogLevel logLevel = LogLevel.Information);

    /// <summary>
    /// Appends a range of log entries to the log.
    /// </summary>
    void AppendRange(IEnumerable<AlterationLogEntry> entries);
    
    /// <summary>
    /// Gets the recorded log entries.
    /// </summary>
    IReadOnlyCollection<AlterationLogEntry> LogEntries { get; }
}