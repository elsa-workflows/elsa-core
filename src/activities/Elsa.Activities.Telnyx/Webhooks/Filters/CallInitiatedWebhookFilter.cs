using System.Reflection;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Telnyx.Webhooks.Services;

namespace Elsa.Activities.Telnyx.Webhooks.Filters
{
    public class CallInitiatedWebhookFilter : IWebhookFilter
    {
        public int Priority => 1;

        public bool CanHandlePayload(Payload payload) => payload is CallInitiatedPayload;

        public string GetActivityTypeName(Payload payload)
        {
            var callInitiated = (CallInitiatedPayload) payload;
            var attribute = payload.GetType().GetCustomAttribute<WebhookAttribute>()!;

            if (callInitiated.State != "bridging")
                return attribute.ActivityType;

            return "BridgeCallInitiated";
        }
    }
}