using Elsa.Common;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Tracks a single in-flight execution cycle of the workflow runtime. Created when a cycle starts, disposed when
/// it completes or is force-cancelled during drain. Active-cycle accounting is done through
/// <see cref="IExecutionCycleRegistry"/>.
/// </summary>
public sealed class ExecutionCycleHandle : IDisposable
{
    private readonly CancellationTokenSource _cycleCts;
    private readonly Action<ExecutionCycleHandle>? _onDisposed;
    private readonly Action? _cancelCallback;
    private readonly TaskCompletionSource _disposedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _cancelled;
    private int _disposed;

    /// <summary>
    /// Creates a new handle. The owning <see cref="IExecutionCycleRegistry"/> supplies <paramref name="onDisposed"/>
    /// so it can decrement its active count. The optional <paramref name="cancelCallback"/> is invoked by
    /// <see cref="Cancel"/> to propagate cancellation into the workflow execution itself (e.g.,
    /// <c>WorkflowExecutionContext.Cancel()</c>) so that the running cycle stops scheduling new activities. Without it,
    /// <see cref="Cancel"/> only signals the cycle's own <see cref="CancellationToken"/>, which is consumed by the
    /// execution-cycle registry but does NOT propagate to the workflow runner's pipeline (the runner reads from
    /// <c>WorkflowExecutionContext.CancellationToken</c>, which is captured at context construction and is not part
    /// of this linked chain).
    /// </summary>
    public ExecutionCycleHandle(
        Guid id,
        string workflowInstanceId,
        string? ingressSourceName,
        DateTimeOffset startedAt,
        CancellationToken linkedToken,
        Action<ExecutionCycleHandle>? onDisposed = null,
        Action? cancelCallback = null)
    {
        Id = id;
        WorkflowInstanceId = workflowInstanceId;
        IngressSourceName = ingressSourceName;
        StartedAt = startedAt;
        _cycleCts = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
        _onDisposed = onDisposed;
        _cancelCallback = cancelCallback;
    }

    /// <summary>Unique identifier for this execution cycle within the current runtime generation.</summary>
    public Guid Id { get; }

    /// <summary>Workflow instance whose execution this cycle is driving.</summary>
    public string WorkflowInstanceId { get; }

    /// <summary>
    /// Name of the ingress source that initiated this cycle, when attribution is available (null for direct API invocations).
    /// </summary>
    public string? IngressSourceName { get; }

    /// <summary>When the cycle started.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Cancellation token passed into the workflow execution pipeline for this cycle.</summary>
    public CancellationToken CancellationToken => _cycleCts.Token;

    /// <summary>
    /// Completes when <see cref="Dispose"/> runs — i.e., when the workflow runner finishes the cycle (cleanly or
    /// via cancellation) and the middleware exits its <c>using</c> block. The drain orchestrator awaits this with
    /// a timeout before persisting <see cref="WorkflowSubStatus.Interrupted"/>, ensuring its write happens AFTER
    /// any commit the runner emits in response to <see cref="Cancel"/>.
    /// </summary>
    public Task Disposed => _disposedTcs.Task;

    /// <summary>
    /// Cancels the cycle — used by the drain orchestrator on deadline breach or operator force.
    /// Invokes the cancel callback (when supplied at construction) to propagate cancellation into the workflow
    /// execution, then cancels the cycle's own linked CTS. Safe to call multiple times; idempotent.
    /// </summary>
    public void Cancel()
    {
        if (_disposed != 0) return;
        // Idempotent guard: ensures the cancel callback and CTS cancellation run AT MOST once even if Cancel() is
        // called repeatedly before disposal. Without this the drain orchestrator (or any other future caller) could
        // accidentally trigger a non-idempotent cancellation side effect multiple times.
        if (Interlocked.Exchange(ref _cancelled, 1) != 0) return;

        // Propagate to the workflow execution first (this typically marks the workflow as Cancelled and clears its
        // schedule, so the runner stops scheduling new activities). The orchestrator's subsequent Interrupted
        // persistence then overrides the Cancelled sub-status — see Disposed-await sequencing in DrainOrchestrator.
        try { _cancelCallback?.Invoke(); }
        catch (Exception ex) when (!ex.IsFatal()) { /* Cancellation is best-effort; non-fatal failures here must not break the drain. */ }

        try { _cycleCts.Cancel(); }
        catch (ObjectDisposedException) { /* Race with Dispose — acceptable. */ }
    }

    /// <summary>Releases the linked CTS, notifies the registry, and signals <see cref="Disposed"/>.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _onDisposed?.Invoke(this);
        _cycleCts.Dispose();
        _disposedTcs.TrySetResult();
    }
}
