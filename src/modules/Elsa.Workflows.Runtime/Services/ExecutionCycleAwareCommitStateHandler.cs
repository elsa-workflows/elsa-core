using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Decorator that disposes the execution-cycle handle stored in
/// <see cref="WorkflowExecutionContext.TransientProperties"/> AFTER the inner commit handler has persisted the
/// workflow's terminal state. This ordering is essential for the drain orchestrator's force-cancel path: the
/// orchestrator awaits <see cref="ExecutionCycleHandle.Disposed"/> with a settle timeout before it overwrites the
/// persisted sub-status with <see cref="WorkflowSubStatus.Interrupted"/>. Without this decorator the handle would
/// dispose at the end of the pipeline middleware (i.e., BEFORE commit), the orchestrator's wait would complete
/// prematurely, and the orchestrator's <c>Interrupted</c> write would either find no instance row (early dispatches)
/// or be clobbered by the runner's subsequent commit (mid-execution dispatches).
/// </summary>
public sealed class ExecutionCycleAwareCommitStateHandler : ICommitStateHandler
{
    private readonly DefaultCommitStateHandler _inner;

    public ExecutionCycleAwareCommitStateHandler(DefaultCommitStateHandler inner) => _inner = inner;

    /// <inheritdoc />
    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        try { await _inner.CommitAsync(workflowExecutionContext, cancellationToken); }
        finally { DisposeCycleHandleIfPresent(workflowExecutionContext); }
    }

    /// <inheritdoc />
    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        try { await _inner.CommitAsync(workflowExecutionContext, workflowState, cancellationToken); }
        finally { DisposeCycleHandleIfPresent(workflowExecutionContext); }
    }

    private static void DisposeCycleHandleIfPresent(WorkflowExecutionContext context)
    {
        if (context.TransientProperties.TryGetValue(ExecutionCycleTrackingMiddleware.ExecutionCycleHandleKey, out var raw) && raw is ExecutionCycleHandle handle)
            handle.Dispose();
    }
}
