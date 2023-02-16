using Elsa.Workflows.Management.Notifications;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
internal class IndexWorkflowTriggersHandler : INotificationHandler<WorkflowDefinitionPublished>, INotificationHandler<WorkflowDefinitionRetracted>
{
    private readonly ITriggerIndexer _triggerIndexer;
    public IndexWorkflowTriggersHandler(ITriggerIndexer triggerIndexer) => _triggerIndexer = triggerIndexer;

    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(notification.WorkflowDefinition, cancellationToken);
    public async Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(notification.WorkflowDefinition, cancellationToken);
}