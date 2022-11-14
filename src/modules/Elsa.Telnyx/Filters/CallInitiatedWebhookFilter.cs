using System.Reflection;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Telnyx.Services;

namespace Elsa.Telnyx.Filters
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