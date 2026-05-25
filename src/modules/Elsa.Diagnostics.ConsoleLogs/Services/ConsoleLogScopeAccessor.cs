using System.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public sealed class ConsoleLogScopeAccessor : ILoggerProvider, ISupportExternalScope
{
    private const string WorkflowInstanceIdKey = "WorkflowInstanceId";
    private readonly IExternalScopeProvider _defaultScopeProvider = new LoggerExternalScopeProvider();
    private readonly object _lock = new();
    private WeakReference<IExternalScopeProvider>[] _scopeProviders;

    public ConsoleLogScopeAccessor()
    {
        _scopeProviders = [new(_defaultScopeProvider)];
    }

    public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
    public void Dispose()
    {
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        lock (_lock)
        {
            var scopeProviders = GetLiveScopeProviders();
            if (scopeProviders.Any(x => ReferenceEquals(x, scopeProvider)))
                return;

            _scopeProviders = [.. _scopeProviders, new WeakReference<IExternalScopeProvider>(scopeProvider)];
        }
    }

    public string? GetWorkflowInstanceId()
    {
        var scopeProviders = GetLiveScopeProviders();
        string? value = null;

        foreach (var scopeProvider in scopeProviders)
        {
            var state = new ScopeSearchState(WorkflowInstanceIdKey);
            scopeProvider.ForEachScope((scope, searchState) => searchState.Capture(scope), state);
            if (state.Value != null)
                value = state.Value;
        }

        return value;
    }

    private IExternalScopeProvider[] GetLiveScopeProviders()
    {
        lock (_lock)
        {
            var liveProviders = new List<IExternalScopeProvider>(_scopeProviders.Length);
            var liveReferences = new List<WeakReference<IExternalScopeProvider>>(_scopeProviders.Length);

            foreach (var scopeProviderReference in _scopeProviders)
            {
                if (!scopeProviderReference.TryGetTarget(out var scopeProvider))
                    continue;

                liveProviders.Add(scopeProvider);
                liveReferences.Add(scopeProviderReference);
            }

            if (liveReferences.Count != _scopeProviders.Length)
                _scopeProviders = [.. liveReferences];

            return [.. liveProviders];
        }
    }

    private sealed class ScopeSearchState(string key)
    {
        public string? Value { get; private set; }

        public void Capture(object? scope)
        {
            var value = TryGetScopeValue(scope, key);
            if (value != null)
                Value = value;
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

            if (value is KeyValuePair<string, object?> kvp)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value?.ToString();

                continue;
            }

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
