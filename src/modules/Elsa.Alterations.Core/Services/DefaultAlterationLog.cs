using System.Collections.ObjectModel;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
public class DefaultAlterationLog : IAlterationLog
{
    private readonly ISystemClock _systemClock;
    private readonly List<AlterationLogEntry> _logEntries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationLog"/> class.
    /// </summary>
    public DefaultAlterationLog(ISystemClock systemClock)
    {
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public void AppendRange(IEnumerable<AlterationLogEntry> entries)
    {
        _logEntries.AddRange(entries);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<AlterationLogEntry> LogEntries => _logEntries.ToList().AsReadOnly();

    /// <inheritdoc />
    public void Append(string message, LogLevel logLevel = LogLevel.Information)
    {
        var entry = new AlterationLogEntry(message, logLevel, _systemClock.UtcNow);
        
        _logEntries.Add(entry);
    }
}