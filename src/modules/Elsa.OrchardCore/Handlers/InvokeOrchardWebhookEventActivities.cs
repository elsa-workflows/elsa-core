using Elsa.Expressions.Helpers;
using Elsa.Mediator.Contracts;
using Elsa.OrchardCore.Stimuli;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Webhooks.Notifications;
using Elsa.Workflows.Runtime;
using WebhooksCore;

namespace Elsa.OrchardCore.Handlers;

/// Invokes Orchard activities based on the received webhook.
public class InvokeOrchardWebhookEventActivities(IStimulusSender stimulusSender) : INotificationHandler<WebhookEventReceived>
{
    public async Task HandleAsync(WebhookEventReceived notification, CancellationToken cancellationToken)
    {
        var webhookEvent = notification.WebhookEvent;

        if (webhookEvent.EventType != EventTypes.ContentItem.Published)
            return;
        
        var contentPublishedPayload = webhookEvent.Payload.ConvertTo<ContentItemPublishedPayload>()!;
        var contentType = contentPublishedPayload.ContentType;
        var fullTypeName = $"Orchard.ContentItem.{contentType}.Published";
        var stimulus = new ContentItemPublishedStimulus(contentType);
        var metadata = new StimulusMetadata
        {
            Input = new Dictionary<string, object>
            {
                [nameof(WebhookEvent)] = webhookEvent,
                [nameof(ContentItemPublishedPayload)] = contentPublishedPayload
            }
        };
        await stimulusSender.SendAsync(fullTypeName, stimulus, metadata, cancellationToken);
    }
}