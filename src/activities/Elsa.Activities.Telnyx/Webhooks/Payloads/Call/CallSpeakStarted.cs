using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Speak Started", "Triggered when speaking has started.")]
    public sealed record CallSpeakStarted : CallPayload
    {
        public const string EventType = "call.speak.started";
        public const string ActivityTypeName = "CallSpeakStarted";
    }
}