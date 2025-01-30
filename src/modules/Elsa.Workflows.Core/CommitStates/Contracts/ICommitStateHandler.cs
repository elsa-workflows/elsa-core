using Elsa.Workflows.State;

namespace Elsa.Workflows.CommitStates;

public interface ICommitStateHandler
{
    Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);
    Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default);
}