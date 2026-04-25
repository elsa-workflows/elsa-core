namespace Elsa.Workflows.Runtime;

/// <summary>
/// Tracks every in-flight burst of workflow execution — the atomic unit of work the drain orchestrator waits on.
/// </summary>
public interface IBurstRegistry
{
    /// <summary>Number of currently active bursts.</summary>
    int ActiveCount { get; }

    /// <summary>
    /// Registers a new burst. Returns the handle that MUST be disposed at burst completion. When an
    /// ingress-source name is supplied and that source's current registry state is <see cref="IngressSourceState.Paused"/>,
    /// this call flips the source to <see cref="IngressSourceState.PauseFailed"/> with reason <c>delivered-while-paused</c>.
    /// </summary>
    BurstHandle BeginBurst(string workflowInstanceId, string? ingressSourceName, CancellationToken linkedToken);

    /// <summary>Enumerates a snapshot of currently active handles — safe to iterate during drain.</summary>
    IReadOnlyCollection<BurstHandle> EnumerateActive();
}
