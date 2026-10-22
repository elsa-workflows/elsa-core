using Elsa.Common;
using Elsa.Extensions;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;

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

        // Lets DispatchWorkflow withdraw this child when the activity is cancelled later in this burst. BulkDispatchWorkflows
        // also waits, but nothing withdraws those children yet; that same-burst case stays with #8821 part 2.
        if (item.WorkflowDefinitionCommand is { InstanceId: { Length: > 0 } childInstanceId } command
            && command.Properties?.TryGetValue<bool>(WorkflowDispatchProperties.WaitForCompletion, out var waitForCompletion) is true && waitForCompletion
            && DispatchWorkflowActivationDeniedRoute.IsForActivityType(command.Properties, ActivityTypeNameHelper.GenerateTypeName<DispatchWorkflow>()))
            workflowExecutionContext.AddPendingChildDispatch(childInstanceId, item.Id);
    }
}
