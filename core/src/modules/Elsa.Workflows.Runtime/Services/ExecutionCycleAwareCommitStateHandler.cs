using Elsa.Workflows.CommitStates;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Compatibility decorator for the default commit handler. Commits are checkpoints and do not end an
/// execution attempt; its <see cref="WorkflowExecutionScope"/> owns execution-cycle cleanup.
/// </summary>
public sealed class ExecutionCycleAwareCommitStateHandler : ICommitStateHandler
{
    private readonly DefaultCommitStateHandler _inner;

    public ExecutionCycleAwareCommitStateHandler(DefaultCommitStateHandler inner) => _inner = inner;

    /// <inheritdoc />
    public Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default) =>
        _inner.CommitAsync(workflowExecutionContext, cancellationToken);

    /// <inheritdoc />
    public Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default) =>
        _inner.CommitAsync(workflowExecutionContext, workflowState, cancellationToken);
}
