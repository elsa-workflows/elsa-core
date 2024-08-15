using Elsa.Expressions.Helpers;
using Elsa.Mediator.Contracts;
using Elsa.OrchardCore.Stimuli;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Webhooks.Notifications;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using WebhooksCore;

namespace Elsa.OrchardCore.Handlers;

/// Invokes Orchard activities based on the received webhook.
[UsedImplicitly]
public class InvokeOrchardWebhookEventActivities(IStimulusSender stimulusSender) : INotificationHandler<WebhookEventReceived>
{
    public async Task HandleAsync(WebhookEventReceived notification, CancellationToken cancellationToken)
    {
        var webhookEvent = notification.WebhookEvent;
        var webhookEventType = webhookEvent.EventType;
        var webhookEventDescriptors = WebhookEventTypes.GetWebhookEventDescriptors().ToDictionary(x => x.WebhookEventType, x => x);
        
        if(!webhookEventDescriptors.TryGetValue(webhookEventType, out var webhookEventDescriptor))
            return;
        
        var eventType = webhookEventDescriptor.EventType;
        var contentItemEventPayload = (ContentItemEventPayload)webhookEvent.Payload.ConvertTo(webhookEventDescriptor.PayloadType)!;
        var contentType = contentItemEventPayload.ContentType;
        var fullTypeName = OrchardCoreActivityNameHelper.GetContentItemEventActivityFullTypeName(contentType, eventType);
        var stimulus = new ContentItemEventStimulus(contentType, eventType);
        var metadata = new StimulusMetadata
        {
            Input = new Dictionary<string, object>
            {
                [nameof(WebhookEvent)] = webhookEvent,
                [nameof(ContentItemEventPayload)] = contentItemEventPayload
            }
        };
        await stimulusSender.SendAsync(fullTypeName, stimulus, metadata, cancellationToken);
    }
}