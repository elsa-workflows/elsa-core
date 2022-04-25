using Elsa.Management.Notifications;
using Elsa.Mediator.Services;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.Handlers;

public class IndexWorkflowTriggers : INotificationHandler<WorkflowPublished>, INotificationHandler<WorkflowRetracted>
{
    private readonly ITriggerIndexer _triggerIndexer;
    public IndexWorkflowTriggers(ITriggerIndexer triggerIndexer) => _triggerIndexer = triggerIndexer;
    public async Task HandleAsync(WorkflowPublished notification, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(notification.Workflow, cancellationToken);
    public async Task HandleAsync(WorkflowRetracted notification, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(notification.Workflow, cancellationToken);
}