using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Handlers;

/// <summary>
/// Deletes workflow instances when a workflow definition or version is deleted.
/// </summary>
public class DeleteWorkflowInstances :
    INotificationHandler<WorkflowDefinitionDeleting>,
    INotificationHandler<WorkflowDefinitionVersionDeleting>,
    INotificationHandler<WorkflowDefinitionsDeleting>,
    INotificationHandler<WorkflowDefinitionVersionsDeleting>
{
    private readonly IWorkflowInstanceManager _workflowInstanceManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteWorkflowInstances"/> class.
    /// </summary>
    public DeleteWorkflowInstances(IWorkflowInstanceManager workflowInstanceManager)
    {
        _workflowInstanceManager = workflowInstanceManager;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteWorkflowInstancesAsync(new WorkflowInstanceFilter { DefinitionId = notification.DefinitionId }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteWorkflowInstancesAsync(new WorkflowInstanceFilter { DefinitionVersionId = notification.WorkflowDefinition.Id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteWorkflowInstancesAsync(new WorkflowInstanceFilter { DefinitionIds = notification.DefinitionIds }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteWorkflowInstancesAsync(new WorkflowInstanceFilter { Ids = notification.Ids }, cancellationToken);
    }

    private async Task DeleteWorkflowInstancesAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken) => await _workflowInstanceManager.BulkDeleteAsync(filter, cancellationToken);
}