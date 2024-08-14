using Elsa.Workflows.State;

namespace Elsa.Workflows;

public interface ICommitStateHandler
{
    Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default);
}