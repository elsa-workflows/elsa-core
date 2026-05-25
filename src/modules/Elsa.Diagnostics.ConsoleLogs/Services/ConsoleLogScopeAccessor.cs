using System.Collections;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public sealed class ConsoleLogScopeAccessor : ILoggerProvider, ISupportExternalScope
{
    private const string WorkflowInstanceIdKey = "WorkflowInstanceId";
    private const int PendingWorkflowLogsCapacity = 1024;
    private static readonly TimeSpan PendingWorkflowLogTtl = TimeSpan.FromSeconds(30);
    private readonly IExternalScopeProvider _defaultScopeProvider = new LoggerExternalScopeProvider();
    private readonly object _lock = new();
    private readonly object _pendingWorkflowLogsLock = new();
    private readonly List<PendingWorkflowLog> _pendingWorkflowLogs = [];
    private WeakReference<IExternalScopeProvider>[] _scopeProviders;

    public ConsoleLogScopeAccessor()
    {
        _scopeProviders = [new(_defaultScopeProvider)];
    }

    public ILogger CreateLogger(string categoryName) => new ScopeCapturingLogger(this);
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

    internal string? TakeLoggedWorkflowInstanceId(string text, DateTimeOffset capturedAt)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        lock (_pendingWorkflowLogsLock)
        {
            PruneExpiredPendingWorkflowLogs(capturedAt);

            var index = _pendingWorkflowLogs.FindIndex(x => IsCorrelated(text, x.Message));
            if (index < 0)
                return null;

            var workflowInstanceId = _pendingWorkflowLogs[index].WorkflowInstanceId;
            _pendingWorkflowLogs.RemoveAt(index);
            return workflowInstanceId;
        }
    }

    private IExternalScopeProvider[] GetLiveScopeProviders()
    {
        lock (_lock)
        {
            var liveScopeProviders = _scopeProviders
                .Select(reference => (Reference: reference, Provider: reference.TryGetTarget(out var provider) ? provider : null))
                .Where(x => x.Provider != null)
                .ToArray();

            if (liveScopeProviders.Length != _scopeProviders.Length)
                _scopeProviders = [.. liveScopeProviders.Select(x => x.Reference)];

            return [.. liveScopeProviders.Select(x => x.Provider!)];
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

    private void CaptureLoggedWorkflowInstanceId<TState>(
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var workflowInstanceId = GetWorkflowInstanceId();
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
            return;

        string message;
        try
        {
            message = formatter(state, exception);
        }
        catch (Exception)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
            return;

        var now = DateTimeOffset.UtcNow;

        lock (_pendingWorkflowLogsLock)
        {
            PruneExpiredPendingWorkflowLogs(now);

            while (_pendingWorkflowLogs.Count >= PendingWorkflowLogsCapacity)
                _pendingWorkflowLogs.RemoveAt(0);

            _pendingWorkflowLogs.Add(new PendingWorkflowLog(message, workflowInstanceId, now));
        }
    }

    private void PruneExpiredPendingWorkflowLogs(DateTimeOffset now)
    {
        for (var i = _pendingWorkflowLogs.Count - 1; i >= 0; i--)
        {
            if (now - _pendingWorkflowLogs[i].CapturedAt <= PendingWorkflowLogTtl)
                continue;

            _pendingWorkflowLogs.RemoveAt(i);
        }
    }

    private static bool IsCorrelated(string capturedLine, string loggedMessage)
    {
        var trimmedLine = capturedLine.Trim();
        var trimmedMessage = loggedMessage.Trim();

        return trimmedLine.Equals(trimmedMessage, StringComparison.Ordinal);
    }

    private sealed class ScopeCapturingLogger(ConsoleLogScopeAccessor accessor) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                accessor.CaptureLoggedWorkflowInstanceId(state, exception, formatter);
        }
    }

    private sealed record PendingWorkflowLog(string Message, string WorkflowInstanceId, DateTimeOffset CapturedAt);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
