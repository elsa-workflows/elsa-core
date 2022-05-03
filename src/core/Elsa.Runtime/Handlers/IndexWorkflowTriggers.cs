using Elsa.Management.Notifications;
using Elsa.Mediator.Services;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.Handlers;

public class IndexWorkflowTriggers : INotificationHandler<WorkflowDefinitionPublished>, INotificationHandler<WorkflowDefinitionRetracted>
{
    private readonly ITriggerIndexer _triggerIndexer;
    public IndexWorkflowTriggers(ITriggerIndexer triggerIndexer) => _triggerIndexer = triggerIndexer;

    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(notification.WorkflowDefinition, cancellationToken);
    public async Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(notification.WorkflowDefinition, cancellationToken);
}