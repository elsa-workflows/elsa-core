using Elsa.Mediator.Contracts;
using Elsa.Webhooks.Models;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;
using WebhooksCore;

namespace Elsa.Webhooks.Handlers;

/// Handles the <see cref="EventNotification"/> notification and asynchronously invokes all registered webhook endpoints.
[UsedImplicitly]
public class WebhookEventNotificationHandler(IWebhookEventBroadcaster webhookDispatcher) : INotificationHandler<EventNotification>
{
    /// <inheritdoc />
    public async Task HandleAsync(EventNotification notification, CancellationToken cancellationToken)
    {
        string eventName = notification.EventName;
        string? correlationId = notification.CorrelationId;
        string? workflowInstanceId = notification.WorkflowInstanceId;
        string? activityInstanceId = notification.ActivityInstanceId;
        object? payload = notification.Payload;
        bool asynchronous = notification.Asynchronous;
        
		var payload = new EventWebhookPayload(
            eventName,
			correlationId,
			workflowInstanceId,
			activityInstanceId,
			payload,
			asynchronous
		);
        
        var webhookEvent = new NewWebhookEvent("Elsa.Event", payload);
        await webhookDispatcher.BroadcastAsync(webhookEvent, cancellationToken);
    }
}