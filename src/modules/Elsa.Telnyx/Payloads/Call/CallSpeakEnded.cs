using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Speak Ended", "Triggered when speaking has ended.")]
    public sealed record CallSpeakEnded : CallPayload
    {
        public const string EventType = "call.speak.ended";
        public const string ActivityTypeName = "CallSpeakEnded";
    }
}