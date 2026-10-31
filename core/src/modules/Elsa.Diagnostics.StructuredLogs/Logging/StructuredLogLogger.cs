using System.Collections;
using System.Diagnostics;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.StructuredLogs.Logging;

public class StructuredLogLogger(
    string categoryName,
    IStructuredLogProvider logProvider,
    IStructuredLogRedactor redactor,
    IStructuredLogSourceRegistry sourceRegistry,
    StructuredLogsOptions options,
    Func<IExternalScopeProvider> getScopeProvider) : ILogger
{
    private long _sequence;
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => getScopeProvider().Push(state);
    
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && (options.IncludeStructuredLogsInternalLogs || !categoryName.StartsWith("Elsa.Diagnostics.StructuredLogs", StringComparison.Ordinal));
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        
        var now = DateTimeOffset.UtcNow;
        var currentActivity = Activity.Current;
        var properties = ExtractProperties(state);
        var scopes = ExtractScopes();
        var logEvent = new StructuredLogEvent
        {
            Sequence = Interlocked.Increment(ref _sequence),
            Timestamp = now,
            ReceivedAt = now,
            Level = ToStructuredLogLevel(logLevel),
            Category = categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = formatter(state, exception),
            MessageTemplate = ExtractMessageTemplate(state),
            Exception = exception == null ? null : new StructuredLogException(exception.GetType().FullName ?? exception.GetType().Name, exception.Message, exception.StackTrace),
            TraceId = currentActivity?.TraceId.ToString(),
            SpanId = currentActivity?.SpanId.ToString(),
            CorrelationId = currentActivity?.RootId ?? GetContextValue(properties, scopes, nameof(StructuredLogEvent.CorrelationId)),
            TenantId = GetContextValue(properties, scopes, nameof(StructuredLogEvent.TenantId)),
            WorkflowDefinitionId = GetContextValue(properties, scopes, nameof(StructuredLogEvent.WorkflowDefinitionId)),
            WorkflowInstanceId = GetContextValue(properties, scopes, nameof(StructuredLogEvent.WorkflowInstanceId)),
            SourceId = sourceRegistry.Current.Id,
            Scopes = scopes,
            Properties = properties
        };
        
        var redacted = redactor.Redact(logEvent);
        _ = logProvider.PublishAsync(redacted);
    }
    
    private static StructuredLogLevel ToStructuredLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => StructuredLogLevel.Trace,
            LogLevel.Debug => StructuredLogLevel.Debug,
            LogLevel.Information => StructuredLogLevel.Information,
            LogLevel.Warning => StructuredLogLevel.Warning,
            LogLevel.Error => StructuredLogLevel.Error,
            LogLevel.Critical => StructuredLogLevel.Critical,
            _ => StructuredLogLevel.None
        };
    }
    
    private static string? ExtractMessageTemplate<TState>(TState state)
    {
        return state is IEnumerable<KeyValuePair<string, object?>> values
            ? values.FirstOrDefault(x => x.Key == "{OriginalFormat}").Value?.ToString()
            : null;
    }

    private static Dictionary<string, string?> ExtractProperties<TState>(TState state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> values)
            return new();
        
        return values
            .Where(x => x.Key != "{OriginalFormat}")
            .ToDictionary(x => x.Key, x => x.Value?.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, string?> ExtractScopes()
    {
        var scopes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var scopeValues = new List<object?>();

        getScopeProvider().ForEachScope((scope, state) => state.Add(scope), scopeValues);

        for (var i = 0; i < scopeValues.Count; i++)
            AddScope(scopes, scopeValues[i], i);

        return scopes;
    }

    private static void AddScope(IDictionary<string, string?> scopes, object? scope, int index)
    {
        if (scope == null)
            return;

        if (TryAddKeyValueScope(scopes, scope))
            return;

        var key = index == 0 ? "Scope" : $"Scope{index + 1}";
        AddValue(scopes, key, scope);
    }

    private static bool TryAddKeyValueScope(IDictionary<string, string?> scopes, object scope)
    {
        if (scope is not IEnumerable values)
            return false;

        var added = false;
        foreach (var value in values)
        {
            if (value == null)
                continue;

            var type = value.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                continue;

            var key = type.GetProperty(nameof(KeyValuePair<string, object?>.Key))?.GetValue(value) as string;
            if (key is null or "{OriginalFormat}")
                continue;

            var itemValue = type.GetProperty(nameof(KeyValuePair<string, object?>.Value))?.GetValue(value);
            AddValue(scopes, key, itemValue);
            added = true;
        }

        return added;
    }

    private static void AddValue(IDictionary<string, string?> values, string key, object? value)
    {
        var uniqueKey = key;
        var suffix = 2;

        while (values.ContainsKey(uniqueKey))
            uniqueKey = $"{key}{suffix++}";

        values[uniqueKey] = value?.ToString();
    }

    private static string? GetContextValue(IReadOnlyDictionary<string, string?> properties, IReadOnlyDictionary<string, string?> scopes, string key)
    {
        return properties.GetValueOrDefault(key) ?? scopes.GetValueOrDefault(key);
    }
}
