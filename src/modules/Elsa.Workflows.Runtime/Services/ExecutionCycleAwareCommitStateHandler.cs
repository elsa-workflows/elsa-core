using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Decorator that disposes the burst handle stored in <see cref="WorkflowExecutionContext.TransientProperties"/>
/// AFTER the inner commit handler has persisted the workflow's terminal state. This ordering is essential for the
/// drain orchestrator's force-cancel path: the orchestrator awaits <c>BurstHandle.Disposed</c> with a settle
/// timeout before it overwrites the persisted sub-status with <see cref="WorkflowSubStatus.Interrupted"/>.
/// Without this decorator the handle would dispose at the end of the pipeline middleware (i.e., BEFORE commit),
/// the orchestrator's wait would complete prematurely, and the orchestrator's <c>Interrupted</c> write would
/// either find no instance row (early dispatches) or be clobbered by the runner's subsequent commit
/// (mid-execution dispatches).
/// </summary>
public sealed class BurstAwareCommitStateHandler : ICommitStateHandler
{
    private readonly DefaultCommitStateHandler _inner;

    public BurstAwareCommitStateHandler(DefaultCommitStateHandler inner) => _inner = inner;

    /// <inheritdoc />
    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        try { await _inner.CommitAsync(workflowExecutionContext, cancellationToken); }
        finally { DisposeBurstHandleIfPresent(workflowExecutionContext); }
    }

    /// <inheritdoc />
    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        try { await _inner.CommitAsync(workflowExecutionContext, workflowState, cancellationToken); }
        finally { DisposeBurstHandleIfPresent(workflowExecutionContext); }
    }

    private static void DisposeBurstHandleIfPresent(WorkflowExecutionContext context)
    {
        if (context.TransientProperties.TryGetValue(BurstTrackingMiddleware.BurstHandleKey, out var raw) && raw is BurstHandle handle)
            handle.Dispose();
    }
}
