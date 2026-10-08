using Elsa.Common;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowDispatchOutbox(
    IWorkflowDispatchOutboxStore store,
    IIdentityGenerator identityGenerator,
    ISystemClock systemClock) : IWorkflowDispatchOutbox
{
    /// <inheritdoc />
    public async Task EnqueueAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default)
    {
        item.Id = identityGenerator.GenerateId();
        item.OwnerWorkflowInstanceId = workflowExecutionContext.Id;
        item.CreatedAt = systemClock.UtcNow;

        await store.SaveAsync(item, cancellationToken);
        workflowExecutionContext.AddWorkflowDispatchOutboxItem(item.Id);
    }
}
