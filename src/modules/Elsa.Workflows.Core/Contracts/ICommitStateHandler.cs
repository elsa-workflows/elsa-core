namespace Elsa.Workflows;

public interface ICommitStateHandler
{
    Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);
}