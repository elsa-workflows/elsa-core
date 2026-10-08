using Elsa.Labels.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Labels.Handlers;

// ReSharper disable once UnusedType.Global
/// <summary>
/// A handler that removes associated workflow labels in response to workflow definitions being deleted. 
/// </summary>
public class DeleteWorkflowLabels : INotificationHandler<WorkflowDefinitionDeleted>, INotificationHandler<WorkflowDefinitionsDeleted>
{
    private readonly IWorkflowDefinitionLabelStore _workflowDefinitionLabelStore;
    public DeleteWorkflowLabels(IWorkflowDefinitionLabelStore workflowDefinitionLabelStore) => _workflowDefinitionLabelStore = workflowDefinitionLabelStore;

    public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) =>
        await _workflowDefinitionLabelStore.DeleteByWorkflowDefinitionIdAsync(notification.DefinitionId, cancellationToken);

    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) =>
        await _workflowDefinitionLabelStore.DeleteByWorkflowDefinitionIdsAsync(notification.DefinitionIds, cancellationToken);
}