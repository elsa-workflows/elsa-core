using Elsa.Workflows.Management;

namespace Elsa.Workflows.Runtime;

public class StoreCommitStateHandler(IWorkflowInstanceManager workflowInstanceManager) : ICommitStateHandler
{
    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        await workflowExecutionContext.ExecuteDeferredTasksAsync();
        await workflowInstanceManager.SaveAsync(workflowExecutionContext, cancellationToken);
    }
}