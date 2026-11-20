using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Compatibility decorator for the default commit handler. Commits are checkpoints and do not end an
/// execution attempt; its <see cref="WorkflowExecutionScope"/> owns execution-cycle cleanup. Commits of a cycle that a
/// host-stop drain interrupted are dropped.
/// </summary>
public sealed class ExecutionCycleAwareCommitStateHandler : ICommitStateHandler
{
    private readonly DefaultCommitStateHandler _inner;

    public ExecutionCycleAwareCommitStateHandler(DefaultCommitStateHandler inner) => _inner = inner;

    /// <inheritdoc />
    public Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default) =>
        IsInterrupted(workflowExecutionContext) ? Task.CompletedTask : _inner.CommitAsync(workflowExecutionContext, cancellationToken);

    /// <inheritdoc />
    public Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default) =>
        IsInterrupted(workflowExecutionContext) ? Task.CompletedTask : _inner.CommitAsync(workflowExecutionContext, workflowState, cancellationToken);

    // Once a host-stop drain has persisted the instance for the next start, the cycle it cut off must not overwrite that.
    private static bool IsInterrupted(WorkflowExecutionContext context) =>
        context.TransientProperties.TryGetValue(ExecutionCycleTrackingMiddleware.ExecutionCycleHandleKey, out var value) && value is ExecutionCycleHandle { IsInterrupted: true };
}
