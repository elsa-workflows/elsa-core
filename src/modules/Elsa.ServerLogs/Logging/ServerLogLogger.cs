using System.Diagnostics;
using Elsa.ServerLogs.Contracts;
using Elsa.ServerLogs.Models;
using Elsa.ServerLogs.Options;
using Microsoft.Extensions.Logging;

namespace Elsa.ServerLogs.Logging;

public class ServerLogLogger(
    string categoryName,
    IServerLogProvider logProvider,
    IServerLogRedactor redactor,
    IServerLogSourceRegistry sourceRegistry,
    ServerLogStreamingOptions options) : ILogger
{
    private long _sequence;
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && (options.IncludeServerLogsInternalLogs || !categoryName.StartsWith("Elsa.ServerLogs", StringComparison.Ordinal));
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        
        var now = DateTimeOffset.UtcNow;
        var currentActivity = Activity.Current;
        var logEvent = new ServerLogEvent
        {
            Sequence = Interlocked.Increment(ref _sequence),
            Timestamp = now,
            ReceivedAt = now,
            Level = ToServerLogLevel(logLevel),
            Category = categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = formatter(state, exception),
            Exception = exception == null ? null : new ServerLogException(exception.GetType().FullName ?? exception.GetType().Name, exception.Message, exception.StackTrace),
            TraceId = currentActivity?.TraceId.ToString(),
            SpanId = currentActivity?.SpanId.ToString(),
            CorrelationId = currentActivity?.RootId,
            SourceId = sourceRegistry.Current.Id,
            Properties = ExtractProperties(state)
        };
        
        var redacted = redactor.Redact(logEvent);
        _ = logProvider.PublishAsync(redacted);
    }
    
    private static ServerLogLevel ToServerLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => ServerLogLevel.Trace,
            LogLevel.Debug => ServerLogLevel.Debug,
            LogLevel.Information => ServerLogLevel.Information,
            LogLevel.Warning => ServerLogLevel.Warning,
            LogLevel.Error => ServerLogLevel.Error,
            LogLevel.Critical => ServerLogLevel.Critical,
            _ => ServerLogLevel.None
        };
    }
    
    private static Dictionary<string, string?> ExtractProperties<TState>(TState state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> values)
            return new();
        
        return values
            .Where(x => x.Key != "{OriginalFormat}")
            .ToDictionary(x => x.Key, x => x.Value?.ToString(), StringComparer.OrdinalIgnoreCase);
    }
    
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        
        public void Dispose()
        {
        }
    }
}
