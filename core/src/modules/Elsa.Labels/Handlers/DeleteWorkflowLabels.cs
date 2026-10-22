using Elsa.Labels.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Labels.Handlers;

// ReSharper disable once UnusedType.Global
/// <summary>
/// Removes associated workflow labels when definitions or individual versions are deleted. 
/// </summary>
public class DeleteWorkflowLabels :
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>
{
    private readonly IWorkflowDefinitionLabelStore _workflowDefinitionLabelStore;
    public DeleteWorkflowLabels(IWorkflowDefinitionLabelStore workflowDefinitionLabelStore) => _workflowDefinitionLabelStore = workflowDefinitionLabelStore;

    public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) =>
        await _workflowDefinitionLabelStore.DeleteByWorkflowDefinitionIdAsync(notification.DefinitionId, cancellationToken);

    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) =>
        await _workflowDefinitionLabelStore.DeleteByWorkflowDefinitionIdsAsync(notification.DefinitionIds, cancellationToken);

    public async Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken) =>
        await _workflowDefinitionLabelStore.DeleteByWorkflowDefinitionVersionIdAsync(notification.WorkflowDefinition.Id, cancellationToken);

    public async Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) =>
        await _workflowDefinitionLabelStore.DeleteByWorkflowDefinitionVersionIdsAsync(notification.Ids, cancellationToken);
}