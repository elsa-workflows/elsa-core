using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Handlers;

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