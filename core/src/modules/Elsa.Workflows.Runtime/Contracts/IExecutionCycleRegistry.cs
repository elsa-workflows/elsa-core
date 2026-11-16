namespace Elsa.Workflows.Runtime;

/// <summary>
/// Tracks every in-flight execution cycle of the workflow runtime — the atomic unit of work the drain orchestrator
/// waits on. An execution cycle is one slice of workflow execution between pipeline entry and the next persistence
/// boundary; the runner may execute many activities within a single cycle.
/// </summary>
public interface IExecutionCycleRegistry
{
    /// <summary>Number of currently active execution cycles.</summary>
    int ActiveCount { get; }

    /// <summary>
    /// Registers a new execution cycle. Returns the handle that MUST be disposed at cycle completion. When an
    /// ingress-source name is supplied and that source's current registry state is <see cref="IngressSourceState.Paused"/>,
    /// this call flips the source to <see cref="IngressSourceState.PauseFailed"/> with reason <c>delivered-while-paused</c>.
    /// The optional <paramref name="cancelCallback"/> is invoked by the drain orchestrator (via
    /// <see cref="ExecutionCycleHandle.Cancel"/>) to propagate cancellation into the workflow execution itself when the
    /// deadline is breached.
    /// </summary>
    ExecutionCycleHandle BeginCycle(string workflowInstanceId, string? ingressSourceName, CancellationToken linkedToken, Action? cancelCallback = null);

    /// <summary>
    /// Like <see cref="BeginCycle(string, string?, CancellationToken, Action?)"/>, but runs <paramref name="configure"/> on the
    /// handle before a drain can list it, so a drain never sees the handle without what <paramref name="configure"/> sets.
    /// The default implementation runs <paramref name="configure"/> after the handle is registered; a registry that lists a
    /// handle as soon as it registers it should override this and run <paramref name="configure"/> first.
    /// </summary>
    ExecutionCycleHandle BeginCycle(string workflowInstanceId, string? ingressSourceName, CancellationToken linkedToken, Action? cancelCallback, Action<ExecutionCycleHandle>? configure)
    {
        var handle = BeginCycle(workflowInstanceId, ingressSourceName, linkedToken, cancelCallback);
        configure?.Invoke(handle);
        return handle;
    }

    /// <summary>Lists a snapshot of currently active execution-cycle handles — safe to iterate during drain.</summary>
    IReadOnlyCollection<ExecutionCycleHandle> ListActiveCycles();
}
