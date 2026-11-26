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
    public Task<DerivedTriggers> GetOrDeriveAsync(string versionId, Func<Task<DerivedTriggers>> derive, ICacheManager? cacheManager) =>
        GetOrRunAsync(GetState(cacheManager).Derived, versionId, derive);

    /// <summary>
    /// Reconciles the shared rows of the version once per process.
    /// </summary>
    public Task EnsureSharedRowsReconciledAsync(string versionId, Func<Task<bool>> reconcile, ICacheManager? cacheManager) =>
        GetOrRunAsync(GetState(cacheManager).ReconciledVersions, versionId, reconcile);

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

    private static async Task<T> GetOrRunAsync<T>(ConcurrentDictionary<string, Lazy<Task<T>>> entries, string key, Func<Task<T>> factory)
    {
        var entry = entries.GetOrAdd(key, _ => new(factory));

        try
        {
            return await entry.Value;
        }
        catch
        {
            // A failed attempt is not remembered, so that the next activation tries again.
            entries.TryRemove(new(key, entry));
            throw;
        }
    }

    private sealed class State(IChangeToken? token)
    {
        public IChangeToken? Token { get; } = token;
        public ConcurrentDictionary<string, Lazy<Task<DerivedTriggers>>> Derived { get; } = new();
        public ConcurrentDictionary<string, Lazy<Task<bool>>> ReconciledVersions { get; } = new();
    }
}
