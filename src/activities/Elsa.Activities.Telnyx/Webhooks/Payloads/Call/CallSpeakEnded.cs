using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Speak Ended", "Triggered when speaking has ended.")]
    public sealed record CallSpeakEnded : CallPayload
    {
        public const string EventType = "call.speak.ended";
        public const string ActivityTypeName = "CallSpeakEnded";
    }
}