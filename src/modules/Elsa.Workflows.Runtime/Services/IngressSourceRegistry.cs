using System.Collections.Concurrent;
using Elsa.Common;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default implementation of <see cref="IIngressSourceRegistry"/>. Backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// keyed by source name, with atomic state transitions.
/// </summary>
public sealed class IngressSourceRegistry : IIngressSourceRegistry
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly ISystemClock _clock;

    /// <summary>Creates the registry and registers every <see cref="IIngressSource"/> resolved from DI.</summary>
    public IngressSourceRegistry(IEnumerable<IIngressSource> sources, ISystemClock clock)
    {
        _clock = clock;
        foreach (var source in sources)
        {
            if (!_entries.TryAdd(source.Name, new Entry(source)))
                throw new InvalidOperationException($"Duplicate ingress source registration for name '{source.Name}'.");
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IIngressSource> Sources => _entries.Values.Select(e => e.Source).ToArray();

    /// <inheritdoc />
    public IReadOnlyCollection<IngressSourceSnapshot> Snapshot()
    {
        return _entries.Values
            .Select(e => new IngressSourceSnapshot(e.Source.Name, e.State, e.LastError, e.LastTransitionAt))
            .ToArray();
    }

    /// <inheritdoc />
    public ValueTask MarkPauseFailedAsync(string name, string reason, Exception? error = null)
    {
        if (_entries.TryGetValue(name, out var entry))
        {
            lock (entry.Sync)
            {
                entry.State = IngressSourceState.PauseFailed;
                entry.LastError = error ?? new InvalidOperationException(reason);
                entry.LastTransitionAt = _clock.UtcNow;
            }
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void RecordTransition(string name, IngressSourceState newState, Exception? error = null)
    {
        if (_entries.TryGetValue(name, out var entry))
        {
            lock (entry.Sync)
            {
                entry.State = newState;
                entry.LastError = error;
                entry.LastTransitionAt = _clock.UtcNow;
            }
        }
    }

    internal IngressSourceState GetState(string name) => _entries.TryGetValue(name, out var e) ? e.State : IngressSourceState.Running;

    private sealed class Entry(IIngressSource source)
    {
        public readonly object Sync = new();
        public IIngressSource Source { get; } = source;
        public IngressSourceState State { get; set; } = IngressSourceState.Running;
        public Exception? LastError { get; set; }
        public DateTimeOffset? LastTransitionAt { get; set; }
    }
}
