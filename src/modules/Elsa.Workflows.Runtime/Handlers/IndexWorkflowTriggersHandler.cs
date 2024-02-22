using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
internal class IndexWorkflowTriggersHandler(ITriggerIndexer triggerIndexer) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>
{
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => await triggerIndexer.IndexTriggersAsync(notification.WorkflowDefinition, cancellationToken);
    public async Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await triggerIndexer.IndexTriggersAsync(notification.WorkflowDefinition, cancellationToken);
    public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => await triggerIndexer.IndexTriggersDeleteWorkflowAsync(new List<string> { notification.DefinitionId }, cancellationToken);
    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => await triggerIndexer.IndexTriggersDeleteWorkflowAsync(notification.DefinitionIds, cancellationToken);
}