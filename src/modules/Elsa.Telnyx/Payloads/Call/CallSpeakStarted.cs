using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Speak Started", "Triggered when speaking has started.")]
    public sealed record CallSpeakStarted : CallPayload
    {
        public const string EventType = "call.speak.started";
        public const string ActivityTypeName = "CallSpeakStarted";
    }
}