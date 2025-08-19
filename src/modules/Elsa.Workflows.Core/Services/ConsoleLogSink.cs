using System.Text.Json;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Elsa.Workflows.Services;

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
    public bool IsEnabled(Models.LogLevel level, string category)
    {
        return _logger.IsEnabled(ConvertToMicrosoftLogLevel(level));
    }

    /// <inheritdoc />
    public Task WriteAsync(ProcessLogEntry entry, CancellationToken cancellationToken = default)
    {
        var microsoftLogLevel = ConvertToMicrosoftLogLevel(entry.Level);
        var eventId = new EventId(entry.EventId ?? 0, entry.Category);
        
        // Create structured log data
        var logData = new
        {
            entry.Message,
            entry.Level,
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

        _logger.Log(microsoftLogLevel, eventId, "ProcessLog: {LogData}", JsonSerializer.Serialize(logData));
        
        return Task.CompletedTask;
    }

    private static MsLogLevel ConvertToMicrosoftLogLevel(Models.LogLevel logLevel)
    {
        return logLevel switch
        {
            Models.LogLevel.Trace => MsLogLevel.Trace,
            Models.LogLevel.Debug => MsLogLevel.Debug,
            Models.LogLevel.Information => MsLogLevel.Information,
            Models.LogLevel.Warning => MsLogLevel.Warning,
            Models.LogLevel.Error => MsLogLevel.Error,
            Models.LogLevel.Critical => MsLogLevel.Critical,
            _ => MsLogLevel.Information
        };
    }
}