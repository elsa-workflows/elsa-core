using System.Reflection;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Services;

namespace Elsa.Activities.Telnyx.Webhooks.Filters
{
    public class AttributeBasedWebhookFilter : IWebhookFilter
    {
        public int Priority => 0;
        public bool CanHandlePayload(Payload payload) => payload.GetType().GetCustomAttribute<WebhookAttribute>() != null;
        public string GetActivityTypeName(Payload payload) => payload.GetType().GetCustomAttribute<WebhookAttribute>()!.ActivityType;
    }
}