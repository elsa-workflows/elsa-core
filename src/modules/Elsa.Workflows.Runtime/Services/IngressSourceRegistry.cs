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

    /// <summary>
    /// Creates the registry. The <paramref name="sourcesFactory"/> is materialised lazily so adapter
    /// implementations can take a direct <see cref="IQuiescenceSignal"/> dependency without creating a DI cycle
    /// (the signal depends on <see cref="IBurstRegistry"/>, which depends on this registry, which depends on
    /// <c>IEnumerable&lt;IIngressSource&gt;</c>). The first call that requires <see cref="Sources"/> materialises
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
            EnsureMaterialised();
            return _entries.Values.Select(e => e.Source).ToArray();
        }
    }

    private void EnsureMaterialised()
    {
        if (_entries.Count > 0) return;
        foreach (var source in _sourcesFactory.Value)
        {
            if (!_entries.TryAdd(source.Name, new Entry(source)))
                throw new InvalidOperationException($"Duplicate ingress source registration for name '{source.Name}'.");
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IngressSourceSnapshot> Snapshot()
    {
        EnsureMaterialised();
        return _entries.Values
            .Select(e => new IngressSourceSnapshot(e.Source.Name, e.State, e.LastError, e.LastTransitionAt))
            .ToArray();
    }

    /// <inheritdoc />
    public ValueTask MarkPauseFailedAsync(string name, string reason, Exception? error = null)
    {
        EnsureMaterialised();
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
        EnsureMaterialised();
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
        EnsureMaterialised();
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
