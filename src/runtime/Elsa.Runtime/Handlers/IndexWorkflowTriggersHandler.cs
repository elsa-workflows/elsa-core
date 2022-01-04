using Elsa.Management.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Handlers;

public class IndexWorkflowTriggersHandler : INotificationHandler<WorkflowPublished>, INotificationHandler<WorkflowRetracted>
{
    private readonly ITriggerIndexer _triggerIndexer;
    public IndexWorkflowTriggersHandler(ITriggerIndexer triggerIndexer) => _triggerIndexer = triggerIndexer;
    public async Task HandleAsync(WorkflowPublished notification, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(cancellationToken);
    public async Task HandleAsync(WorkflowRetracted notification, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(cancellationToken);
}