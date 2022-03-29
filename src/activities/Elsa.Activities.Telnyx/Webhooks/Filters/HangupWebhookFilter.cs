using System.Reflection;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Telnyx.Webhooks.Services;

namespace Elsa.Activities.Telnyx.Webhooks.Filters
{
    public class HangupWebhookFilter : IWebhookFilter
    {
        public int Priority => 1;

        public bool CanHandlePayload(Payload payload) => payload is CallHangupPayload;

        public string GetActivityTypeName(Payload payload)
        {
            var hangupPayload = (CallHangupPayload) payload;
            var attribute = payload.GetType().GetCustomAttribute<WebhookAttribute>()!;

            if(hangupPayload.HangupSource != "caller" || hangupPayload.HangupCause != "normal_clearing")
                return attribute.ActivityType;

            return "OriginatorCallHangup";
        }
    }
}