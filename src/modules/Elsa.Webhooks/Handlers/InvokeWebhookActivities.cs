using Elsa.Mediator.Contracts;
using Elsa.Webhooks.Notifications;
using Elsa.Webhooks.Stimuli;
using Elsa.Workflows.Runtime;
using WebhooksCore;

namespace Elsa.Webhooks.Handlers;

/// <summary>
/// Invokes webhook activities.
/// </summary>
public class InvokeWebhookActivities(IStimulusSender stimulusSender) : INotificationHandler<WebhookEventReceived>
{
    public async Task HandleAsync(WebhookEventReceived notification, CancellationToken cancellationToken)
    {
        var matchingSource = notification.WebhookSource;
        var webhookEvent = notification.WebhookEvent;
        var fullTypeName = matchingSource.GetWebhookActivityTypeName(webhookEvent.EventType);
        var stimulus = new WebhookEventReceivedStimulus(webhookEvent.EventType);
        var input = new Dictionary<string, object>
        {
            [nameof(WebhookEvent)] = webhookEvent
        };
        var metadata = new StimulusMetadata
        {
            Input = input
        };
        await stimulusSender.SendAsync(fullTypeName, stimulus, metadata, cancellationToken);
    }
}