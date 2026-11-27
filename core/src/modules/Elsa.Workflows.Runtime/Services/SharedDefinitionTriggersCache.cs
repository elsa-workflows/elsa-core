using System.Collections.Concurrent;
using Elsa.Caching;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Primitives;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Remembers, per process, the triggers derived from each published version of a tenant-agnostic workflow definition and the versions
/// whose shared rows this process has reconciled, so that tenant activations do not repeat that work for every tenant. It only avoids
/// work: every entry can be recomputed, and each node of a cluster keeps its own.
/// </summary>
/// <remarks>
/// Publishing, retracting or deleting a tenant-agnostic definition clears it, here and, through the <see cref="ICacheManager"/> token
/// <see cref="InvalidationTokenKey"/>, on the other nodes.
/// </remarks>
internal sealed class SharedDefinitionTriggersCache
{
    internal const string InvalidationTokenKey = "Elsa.Workflows.Runtime.SharedDefinitionTriggers";

    private State _state = new(null);
    private long _epoch;
    private readonly ConcurrentDictionary<string, long> _versionGenerations = new();

    /// <summary>
    /// The triggers of a published tenant-agnostic definition version: the shared ones, stamped <c>*</c>, and the scheduled ones, which
    /// every tenant gets a copy of. Callers copy them before they store them.
    /// </summary>
    internal sealed record DerivedTriggers(Workflow Workflow, IReadOnlyCollection<StoredTrigger> SharedTriggers, IReadOnlyCollection<StoredTrigger> ScheduledTriggers);

    /// <summary>
    /// Identifies what this process knew about a version when a caller started reading it. Forgetting the version, or everything, moves it
    /// on, so that what was derived from an earlier read is neither admitted nor returned afterwards.
    /// </summary>
    internal readonly record struct Generation(long Epoch, long Version);

    /// <summary>
    /// Returns the current generation of the version. A caller takes it before it reads the definition it derives from.
    /// </summary>
    public Generation GetGeneration(string versionId) => new(Interlocked.Read(ref _epoch), _versionGenerations.GetValueOrDefault(versionId));

    /// <summary>
    /// Returns whether the triggers of the version have been derived in this process.
    /// </summary>
    public bool ContainsDerived(string versionId, ICacheManager? cacheManager) =>
        GetState(cacheManager).Derived.TryGetValue(versionId, out var entry) && entry.Generation == GetGeneration(versionId);

    /// <summary>
    /// Returns the triggers of the version, deriving them once per process.
    /// </summary>
    /// <remarks>
    /// The derivation is shared by every caller and does not run on any one caller's token; each caller stops waiting when its own
    /// <paramref name="cancellationToken"/> is cancelled. A caller whose <paramref name="generation"/> is no longer current read the
    /// definition before the version was forgotten: its derivation is returned to it but neither stored nor shared.
    /// </remarks>
    /// <param name="versionId">The version.</param>
    /// <param name="generation">The generation the caller took before it read the definition that <paramref name="derive"/> derives from.</param>
    /// <param name="derive">Derives the triggers.</param>
    /// <param name="cacheManager">The cache manager, if any, whose token tells this node that another node cleared its cache.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task<DerivedTriggers> GetOrDeriveAsync(string versionId, Generation generation, Func<CancellationToken, Task<DerivedTriggers>> derive, ICacheManager? cacheManager, CancellationToken cancellationToken) =>
        GetOrRunAsync(GetState(cacheManager).Derived, versionId, generation, derive, cancellationToken);

    /// <summary>
    /// Reconciles the shared rows of the version once per process and returns whether they were written.
    /// </summary>
    /// <remarks>
    /// The reconciliation is shared by every caller and does not run on any one caller's token; each caller stops waiting when its own
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </remarks>
    public Task<bool> EnsureSharedRowsReconciledAsync(string versionId, Func<CancellationToken, Task<bool>> reconcile, ICacheManager? cacheManager, CancellationToken cancellationToken) =>
        GetOrRunAsync(GetState(cacheManager).ReconciledVersions, versionId, GetGeneration(versionId), reconcile, cancellationToken);

    /// <summary>
    /// Forgets what this node derived and reconciled for the version, for example after a provider import changed it in place. Every node
    /// imports the provider itself, so the other nodes are not told.
    /// </summary>
    public void Forget(string versionId)
    {
        // The generation moves on first, so that a derivation started from an earlier read is not admitted after the entries are removed.
        _versionGenerations.AddOrUpdate(versionId, 1, (_, generation) => generation + 1);
        var state = _state;
        state.Derived.TryRemove(versionId, out _);
        state.ReconciledVersions.TryRemove(versionId, out _);
    }

    /// <summary>
    /// Forgets everything, on this node and, when there is a cache manager, on the others.
    /// </summary>
    public async Task InvalidateAsync(ICacheManager? cacheManager, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _epoch);
        _state = new(null);

        if (cacheManager != null)
            await cacheManager.TriggerTokenAsync(InvalidationTokenKey, cancellationToken);
    }

    private State GetState(ICacheManager? cacheManager)
    {
        var state = _state;

        if (state.Token is { HasChanged: false } || state.Token == null && cacheManager == null)
            return state;

        // Another node cleared its cache (or this one never had a token yet): what was read before is not admitted afterwards.
        if (Interlocked.CompareExchange(ref _state, new(cacheManager?.GetToken(InvalidationTokenKey)), state) == state && state.Token != null)
            Interlocked.Increment(ref _epoch);

        return _state;
    }

    private async Task<T> GetOrRunAsync<T>(ConcurrentDictionary<string, Entry<T>> entries, string key, Generation generation, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        while (true)
        {
            Entry<T>? ownEntry = null;
            var entry = entries.GetOrAdd(key, _ => ownEntry = new(generation, new(() => factory(CancellationToken.None))));
            var currentGeneration = GetGeneration(key);

            if (entry.Generation != currentGeneration)
            {
                // Stored from a read that the version was forgotten after: not returned to anyone.
                entries.TryRemove(new(key, entry));

                // This caller's own read came before the version was forgotten: it gets its derivation, which nobody else sees.
                if (generation != currentGeneration)
                    return await factory(cancellationToken);

                continue;
            }

            try
            {
                return await entry.Task.Value.WaitAsync(cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // A failed attempt is not remembered, so that the next caller tries again.
                entries.TryRemove(new(key, entry));

                // An attempt started by another caller can fail because that caller went away, for example when its scope was disposed
                // after its activation was cancelled. Try again with this caller's services instead of failing with it.
                if (ReferenceEquals(entry, ownEntry))
                    throw;
            }
        }
    }

    private sealed record Entry<T>(Generation Generation, Lazy<Task<T>> Task);

    private sealed class State(IChangeToken? token)
    {
        public IChangeToken? Token { get; } = token;
        public ConcurrentDictionary<string, Entry<DerivedTriggers>> Derived { get; } = new();
        public ConcurrentDictionary<string, Entry<bool>> ReconciledVersions { get; } = new();
    }
}
