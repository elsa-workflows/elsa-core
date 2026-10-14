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

        // Lets a dispatching activity cancelled later in this burst withdraw the child before it is ever created.
        if (item.WorkflowDefinitionCommand?.InstanceId is { Length: > 0 } childInstanceId)
            workflowExecutionContext.AddPendingChildDispatch(childInstanceId, item.Id);
    }
}
