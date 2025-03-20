using Elsa.Common;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Models;


/// <summary>
/// Represents a log of alterations.
/// </summary>
public class AlterationLog(ISystemClock systemClock)
{
    private readonly List<AlterationLogEntry> _logEntries = new();

    /// <summary>
    /// Gets the log entries.
    /// </summary>
    public IReadOnlyCollection<AlterationLogEntry> LogEntries => _logEntries.ToList().AsReadOnly();

    /// <summary>
    /// Adds a log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventName">The event that generated the log entry.</param>
    public void Add(string message, LogLevel logLevel = LogLevel.Information, string? eventName = null)
    {
        var entry = new AlterationLogEntry(message, logLevel, _systemClock.UtcNow, eventName);
        
        _logEntries.Add(entry);
    }
    
    /// <summary>
    /// Adds a set of log entries.
    /// </summary>
    /// <param name="entries"></param>
    public void AddRange(IEnumerable<AlterationLogEntry> entries) => _logEntries.AddRange(entries);
}