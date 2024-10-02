using Elsa.Mediator.Contracts;
using Elsa.Webhooks.Models;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;
using WebhooksCore;

namespace Elsa.Webhooks.Handlers;

/// Handles the <see cref="DomainEventNotification"/> notification and asynchronously invokes all registered webhook endpoints.
[UsedImplicitly]
public class DomainEventHandler(IWebhookEventBroadcaster webhookDispatcher) : INotificationHandler<DomainEventNotification>
{
    /// <inheritdoc />
    public async Task HandleAsync(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        var activityExecutionContext = notification.ActivityExecutionContext;
        var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
        var workflowInstanceId = workflowExecutionContext.Id;
        var correlationId = workflowExecutionContext.CorrelationId;
        var workflow = workflowExecutionContext.Workflow;
        var workflowDefinitionId = workflow.Identity.DefinitionId;
        var workflowName = workflow.WorkflowMetadata.Name;
        
        var payload = new DomainEventWebhookPayload(
            workflowInstanceId,
            workflowDefinitionId,
            workflowName,
            correlationId,
            notification.DomainEventId, 
            notification.DomainEventName, 
            notification.DomainEventPayload
        );
        
        var webhookEvent = new NewWebhookEvent("Elsa.DomainEvent", payload);
        await webhookDispatcher.BroadcastAsync(webhookEvent, cancellationToken);
    }
}