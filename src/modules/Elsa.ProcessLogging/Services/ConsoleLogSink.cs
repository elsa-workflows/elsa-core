using System.Text.Json;
using Elsa.Logging.Contracts;
using Elsa.Logging.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Services;

/// <summary>
/// A log sink that writes entries to the console.
/// </summary>
public class ConsoleLogSink : ILogSink
{
    private readonly ILogger<ConsoleLogSink> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogSink"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ConsoleLogSink(ILogger<ConsoleLogSink> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "console";

    /// <inheritdoc />
    public bool IsEnabled(LogLevel level, string category)
    {
        return _logger.IsEnabled(level);
    }

    /// <inheritdoc />
    public Task WriteAsync(ProcessLogEntry entry, CancellationToken cancellationToken = default)
    {
        var eventId = new EventId(entry.EventId ?? 0, entry.Category);

        // Create structured log data
        var logData = new
        {
            entry.Message,
            Level = entry.LogLevel,
            entry.Category,
            entry.EventId,
            entry.WorkflowInstanceId,
            entry.WorkflowName,
            entry.ActivityId,
            entry.ActivityName,
            entry.CorrelationId,
            entry.Timestamp,
            Attributes = entry.Attributes
        };

        _logger.Log(entry.LogLevel, eventId, "ProcessLog: {LogData}", JsonSerializer.Serialize(logData));

        return Task.CompletedTask;
    }
}