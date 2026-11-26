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

    /// <summary>
    /// The triggers of a published tenant-agnostic definition version: the shared ones, stamped <c>*</c>, and the scheduled ones, which
    /// every tenant gets a copy of. Callers copy them before they store them.
    /// </summary>
    internal sealed record DerivedTriggers(Workflow Workflow, IReadOnlyCollection<StoredTrigger> SharedTriggers, IReadOnlyCollection<StoredTrigger> ScheduledTriggers);

    /// <summary>
    /// Returns whether the triggers of the version have been derived in this process.
    /// </summary>
    public bool ContainsDerived(string versionId, ICacheManager? cacheManager) => GetState(cacheManager).Derived.ContainsKey(versionId);

    /// <summary>
    /// Returns the triggers of the version, deriving them once per process.
    /// </summary>
    /// <remarks>
    /// The derivation is shared by every caller and does not run on any one caller's token; each caller stops waiting when its own
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </remarks>
    public Task<DerivedTriggers> GetOrDeriveAsync(string versionId, Func<CancellationToken, Task<DerivedTriggers>> derive, ICacheManager? cacheManager, CancellationToken cancellationToken) =>
        GetOrRunAsync(GetState(cacheManager).Derived, versionId, derive, cancellationToken);

    /// <summary>
    /// Reconciles the shared rows of the version once per process and returns whether they were written.
    /// </summary>
    /// <remarks>
    /// The reconciliation is shared by every caller and does not run on any one caller's token; each caller stops waiting when its own
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </remarks>
    public Task<bool> EnsureSharedRowsReconciledAsync(string versionId, Func<CancellationToken, Task<bool>> reconcile, ICacheManager? cacheManager, CancellationToken cancellationToken) =>
        GetOrRunAsync(GetState(cacheManager).ReconciledVersions, versionId, reconcile, cancellationToken);

    /// <summary>
    /// Forgets everything, on this node and, when there is a cache manager, on the others.
    /// </summary>
    public async Task InvalidateAsync(ICacheManager? cacheManager, CancellationToken cancellationToken)
    {
        _state = new(null);

        if (cacheManager != null)
            await cacheManager.TriggerTokenAsync(InvalidationTokenKey, cancellationToken);
    }

    private State GetState(ICacheManager? cacheManager)
    {
        var state = _state;

        if (state.Token is { HasChanged: false } || state.Token == null && cacheManager == null)
            return state;

        Interlocked.CompareExchange(ref _state, new(cacheManager?.GetToken(InvalidationTokenKey)), state);
        return _state;
    }

    private static async Task<T> GetOrRunAsync<T>(ConcurrentDictionary<string, Lazy<Task<T>>> entries, string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        while (true)
        {
            Lazy<Task<T>>? ownEntry = null;
            var entry = entries.GetOrAdd(key, _ => ownEntry = new(() => factory(CancellationToken.None)));

            try
            {
                return await entry.Value.WaitAsync(cancellationToken);
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

    private sealed class State(IChangeToken? token)
    {
        public IChangeToken? Token { get; } = token;
        public ConcurrentDictionary<string, Lazy<Task<DerivedTriggers>>> Derived { get; } = new();
        public ConcurrentDictionary<string, Lazy<Task<bool>>> ReconciledVersions { get; } = new();
    }
}
