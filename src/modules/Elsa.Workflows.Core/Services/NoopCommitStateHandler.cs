using Elsa.Workflows.State;

namespace Elsa.Workflows;

public class NoopCommitStateHandler : ICommitStateHandler
{
    public Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}