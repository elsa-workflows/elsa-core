using Elsa.Common;
using Elsa.Extensions;
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

        // Lets the activity waiting for this child withdraw it when the activity is cancelled later in this burst.
        if (item.WorkflowDefinitionCommand is { InstanceId: { Length: > 0 } childInstanceId } command
            && command.Properties?.TryGetValue<bool>("WaitForCompletion", out var waitForCompletion) == true && waitForCompletion)
            workflowExecutionContext.AddPendingChildDispatch(childInstanceId, item.Id);
    }
}
