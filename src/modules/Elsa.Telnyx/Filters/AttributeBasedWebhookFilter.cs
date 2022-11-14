using System.Reflection;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Telnyx.Services;

namespace Elsa.Telnyx.Filters
{
    public class AttributeBasedWebhookFilter : IWebhookFilter
    {
        public int Priority => 0;
        public bool CanHandlePayload(Payload payload) => payload.GetType().GetCustomAttribute<WebhookAttribute>() != null;
        public string GetActivityTypeName(Payload payload) => payload.GetType().GetCustomAttribute<WebhookAttribute>()!.ActivityType;
    }
}