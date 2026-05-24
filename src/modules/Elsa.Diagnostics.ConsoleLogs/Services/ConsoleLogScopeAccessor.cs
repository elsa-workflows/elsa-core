using System.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public sealed class ConsoleLogScopeAccessor : ILoggerProvider, ISupportExternalScope
{
    private const string WorkflowInstanceIdKey = "WorkflowInstanceId";
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
    public void Dispose()
    {
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public string? GetWorkflowInstanceId()
    {
        var state = new ScopeSearchState(WorkflowInstanceIdKey);
        _scopeProvider.ForEachScope((scope, searchState) => searchState.Capture(scope), state);
        return state.Value;
    }

    private sealed class ScopeSearchState(string key)
    {
        public string? Value { get; private set; }

        public void Capture(object? scope)
        {
            if (Value != null)
                return;

            Value = TryGetScopeValue(scope, key);
        }
    }

    private static string? TryGetScopeValue(object? scope, string key)
    {
        if (scope is not IEnumerable values)
            return null;

        foreach (var value in values)
        {
            if (value == null)
                continue;

            var type = value.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                continue;

            var itemKey = type.GetProperty(nameof(KeyValuePair<string, object?>.Key))?.GetValue(value) as string;
            if (!string.Equals(itemKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            return type.GetProperty(nameof(KeyValuePair<string, object?>.Value))?.GetValue(value)?.ToString();
        }

        return null;
    }
}
