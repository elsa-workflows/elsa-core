namespace Elsa.Workflows.Runtime;

/// <summary>
/// Central inventory of ingress sources discovered via DI and surfaced to the drain orchestrator,
/// the admin status endpoint, and the burst-attribution detector.
/// </summary>
public interface IIngressSourceRegistry
{
    /// <summary>All registered sources.</summary>
    IReadOnlyCollection<IIngressSource> Sources { get; }

    /// <summary>Point-in-time snapshot of every source's current state.</summary>
    IReadOnlyCollection<IngressSourceSnapshot> Snapshot();

    /// <summary>
    /// Atomically flips a source's recorded state to <see cref="IngressSourceState.PauseFailed"/> and captures the
    /// supplied reason and error. Used by the orchestrator when a pause times out and by <c>IBurstRegistry</c>
    /// when a source claiming <see cref="IngressSourceState.Paused"/> initiates a burst anyway.
    /// </summary>
    ValueTask MarkPauseFailedAsync(string name, string reason, Exception? error = null);

    /// <summary>Records a state transition observed by the orchestrator (pausing/paused/resuming/running).</summary>
    void RecordTransition(string name, IngressSourceState newState, Exception? error = null);
}
