using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Handlers.Notification;

/// <summary>
/// Updates consuming workflows when a workflow definition is published.
/// </summary>
public class UpdateConsumingWorkflows(IWorkflowReferenceUpdater workflowReferenceUpdater) : INotificationHandler<WorkflowDefinitionPublished>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        var definition = notification.WorkflowDefinition;
        var result = await workflowReferenceUpdater.UpdateWorkflowReferencesAsync(definition, cancellationToken);
        notification.AffectedWorkflows.WorkflowDefinitions.AddRange(result.UpdatedWorkflows);
    }
}