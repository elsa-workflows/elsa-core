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
    private readonly object _cycleCtsGate = new();
    private bool _cycleCtsCancellationInProgress;
    private bool _cycleCtsDisposeRequested;
    private bool _cycleCtsDisposed;
    private int _lifecycleState;

    private const int ActiveState = 0;
    private const int CancellingState = 1;
    private const int CancelledState = 2;
    private const int DisposedState = 3;

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
    /// Completes after <see cref="Dispose"/> logically releases the handle and physically cleans up its linked CTS —
    /// i.e., when the workflow runner finishes the cycle (cleanly or via cancellation) and the middleware exits its
    /// <c>using</c> block. If cancellation callbacks are in flight, <see cref="Dispose"/> may return before this
    /// cleanup completes. The drain orchestrator awaits this with a timeout before persisting
    /// <see cref="WorkflowSubStatus.Interrupted"/>, ensuring its write happens AFTER any commit the runner emits in
    /// response to <see cref="Cancel"/>.
    /// </summary>
    public Task Disposed => _disposedTcs.Task;

    /// <summary>
    /// Cancels the cycle — used by the drain orchestrator on deadline breach or operator force.
    /// Invokes the cancel callback (when supplied at construction) to propagate cancellation into the workflow
    /// execution, then cancels the cycle's own linked CTS. Safe to call multiple times; idempotent.
    /// </summary>
    public void Cancel() => TryCancel();

    /// <summary>
    /// Attempts to cancel the cycle. Returns <c>true</c> only when this call transitioned the handle from
    /// active to cancelled. Returns <c>false</c> when the handle was already disposed or already cancelling/cancelled,
    /// so drain can avoid treating a finished cycle as a force-cancel. Disposal wins if it races with the cancellation
    /// callback, so a cycle that completes while cancellation is in flight is not reported as drain-cancelled.
    /// </summary>
    public bool TryCancel()
    {
        if (Interlocked.CompareExchange(ref _lifecycleState, CancellingState, ActiveState) != ActiveState)
            return false;

        // Propagate to the workflow execution first (this typically marks the workflow as Cancelled and clears its
        // schedule, so the runner stops scheduling new activities). The orchestrator's subsequent Interrupted
        // persistence then overrides the Cancelled sub-status — see Disposed-await sequencing in DrainOrchestrator.
        try { _cancelCallback?.Invoke(); }
        catch (Exception ex) when (!ex.IsFatal()) { /* Cancellation is best-effort; non-fatal failures here must not break the drain. */ }

        lock (_cycleCtsGate)
            _cycleCtsCancellationInProgress = true;

        try
        {
            _cycleCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Dispose may have won before cancellation propagation started.
        }
        finally
        {
            lock (_cycleCtsGate)
            {
                _cycleCtsCancellationInProgress = false;
                DisposeCycleCtsIfSafe();
            }
        }

        // Publish cancellation only after its effects complete. Dispose can transition CancellingState directly to
        // DisposedState, making this CAS fail when the cycle completed during the callback or CTS cancellation.
        return Interlocked.CompareExchange(ref _lifecycleState, CancelledState, CancellingState) == CancellingState;
    }

    /// <summary>
    /// Logically releases the handle and notifies the registry. If cancellation callbacks are in flight, this method
    /// may return before physical cleanup of the linked CTS completes; <see cref="Disposed"/> is signaled afterwards.
    /// </summary>
    public void Dispose()
    {
        while (true)
        {
            var state = Volatile.Read(ref _lifecycleState);
            if (state == DisposedState) return;
            if (Interlocked.CompareExchange(ref _lifecycleState, DisposedState, state) == state) break;
        }

        _onDisposed?.Invoke(this);
        lock (_cycleCtsGate)
        {
            _cycleCtsDisposeRequested = true;
            DisposeCycleCtsIfSafe();
        }
    }

    private void DisposeCycleCtsIfSafe()
    {
        if (!_cycleCtsDisposeRequested || _cycleCtsCancellationInProgress || _cycleCtsDisposed)
            return;

        _cycleCtsDisposed = true;
        _cycleCts.Dispose();
        _disposedTcs.TrySetResult();
    }
}
