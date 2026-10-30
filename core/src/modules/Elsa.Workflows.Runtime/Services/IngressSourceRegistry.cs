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
    private readonly Lazy<IEnumerable<IIngressSource>> _sourcesFactory;
    private readonly ISystemClock _clock;
    private readonly object _materializeSync = new();
    private volatile bool _materialized;

    /// <summary>
    /// Creates the registry. The <paramref name="sourcesFactory"/> is materialized lazily so adapter
    /// implementations can take a direct <see cref="IQuiescenceSignal"/> dependency without creating a DI cycle
    /// (the signal depends on <see cref="IExecutionCycleRegistry"/>, which depends on this registry, which depends on
    /// <c>IEnumerable&lt;IIngressSource&gt;</c>). The first call that requires <see cref="Sources"/> materializes
    /// the enumerable; subsequent calls reuse the snapshot.
    /// </summary>
    public IngressSourceRegistry(Lazy<IEnumerable<IIngressSource>> sourcesFactory, ISystemClock clock)
    {
        _sourcesFactory = sourcesFactory;
        _clock = clock;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IIngressSource> Sources
    {
        get
        {
            EnsureMaterialized();
            return _entries.Values.Select(e => e.Source).ToArray();
        }
    }

    private void EnsureMaterialized()
    {
        // Double-checked lock: a `_entries.Count > 0` guard is not atomic with the population loop, so concurrent
        // first callers could both observe an empty dictionary, both iterate _sourcesFactory.Value, and both call
        // TryAdd — losing the duplicate-name guarantee and crashing the second caller's execution cycle start with
        // "Duplicate ingress source registration". A short lock around the one-time population is sufficient;
        // steady-state reads short-circuit on the volatile flag without taking the lock.
        if (_materialized) return;
        lock (_materializeSync)
        {
            if (_materialized) return;
            foreach (var source in _sourcesFactory.Value)
            {
                if (!_entries.TryAdd(source.Name, new Entry(source)))
                    throw new InvalidOperationException($"Duplicate ingress source registration for name '{source.Name}'.");
            }
            _materialized = true;
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IngressSourceSnapshot> Snapshot()
    {
        EnsureMaterialized();
        return _entries.Values
            .Select(e => new IngressSourceSnapshot(e.Source.Name, e.State, e.LastError, e.LastTransitionAt))
            .ToArray();
    }

    /// <inheritdoc />
    public ValueTask MarkPauseFailedAsync(string name, string reason, Exception? error = null)
    {
        EnsureMaterialized();
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
        EnsureMaterialized();
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

    internal IngressSourceState GetState(string name)
    {
        EnsureMaterialized();
        return _entries.TryGetValue(name, out var e) ? e.State : IngressSourceState.Running;
    }

    private sealed class Entry(IIngressSource source)
    {
        public readonly object Sync = new();
        public IIngressSource Source { get; } = source;
        public IngressSourceState State { get; set; } = IngressSourceState.Running;
        public Exception? LastError { get; set; }
        public DateTimeOffset? LastTransitionAt { get; set; }
    }
}
