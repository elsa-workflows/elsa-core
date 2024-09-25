using Elsa.OrchardCore.Models;
using Elsa.OrchardCore.WebhookPayloads;
using JetBrains.Annotations;

namespace Elsa.OrchardCore;

[UsedImplicitly]
public static class WebhookEventTypes
{
    public static IEnumerable<WebhookEventDescriptor> GetWebhookEventDescriptors()
    {
        yield return GetWebhookEventDescriptor("content-item.created", "Created", typeof(ContentItemEventPayload));
        yield return GetWebhookEventDescriptor("content-item.published", "Published", typeof(ContentItemEventPayload));
        yield return GetWebhookEventDescriptor("content-item.unpublished", "Unpublished", typeof(ContentItemEventPayload));
        yield return GetWebhookEventDescriptor("content-item.removed", "Removed", typeof(ContentItemEventPayload));
    }

    private static WebhookEventDescriptor GetWebhookEventDescriptor(string webhookEventType, string eventType, Type payloadType)
    {
        return  new WebhookEventDescriptor
        {
            WebhookEventType = webhookEventType,
            EventType = eventType,
            PayloadType = payloadType
        }; 
    }
}