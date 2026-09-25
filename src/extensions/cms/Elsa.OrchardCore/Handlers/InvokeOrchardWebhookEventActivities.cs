using System.Diagnostics.CodeAnalysis;
using Elsa.Expressions.Helpers;
using Elsa.OrchardCore.Helpers;
using Elsa.OrchardCore.Stimuli;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Mediator.Contracts;
using Elsa.Http.Webhooks.Notifications;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using WebhooksCore;

namespace Elsa.OrchardCore.Handlers;

/// <summary>
/// Invokes Orchard activities based on the received webhook.
/// </summary>
[UsedImplicitly]
[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
public class InvokeOrchardWebhookEventActivities(IStimulusSender stimulusSender) : INotificationHandler<WebhookEventReceived>
{
    /// <inheritdoc />
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