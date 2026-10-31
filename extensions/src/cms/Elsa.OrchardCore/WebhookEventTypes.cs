using Elsa.OrchardCore.Models;
using Elsa.OrchardCore.WebhookPayloads;
using JetBrains.Annotations;

namespace Elsa.OrchardCore;

/// <summary>
/// Provides a collection of webhook event types with corresponding descriptors.
/// This static class defines specific webhook events and their associated details such as type and payload.
/// </summary>
[UsedImplicitly]
public static class WebhookEventTypes
{
    /// <summary>
    /// Retrieves a collection of webhook event descriptors, each representing metadata about specific webhook events.
    /// </summary>
    /// <returns>
    /// An enumerable collection of <see cref="WebhookEventDescriptor"/> instances.
    /// </returns>
    public static IEnumerable<WebhookEventDescriptor> GetWebhookEventDescriptors()
    {
        yield return GetWebhookEventDescriptor("content-item.created", "Created", typeof(ContentItemEventPayload));
        yield return GetWebhookEventDescriptor("content-item.published", "Published", typeof(ContentItemEventPayload));
        yield return GetWebhookEventDescriptor("content-item.unpublished", "Unpublished", typeof(ContentItemEventPayload));
        yield return GetWebhookEventDescriptor("content-item.removed", "Removed", typeof(ContentItemEventPayload));
    }

    private static WebhookEventDescriptor GetWebhookEventDescriptor(string webhookEventType, string eventType, Type payloadType)
    {
        return  new()
        {
            WebhookEventType = webhookEventType,
            EventType = eventType,
            PayloadType = payloadType
        }; 
    }
}