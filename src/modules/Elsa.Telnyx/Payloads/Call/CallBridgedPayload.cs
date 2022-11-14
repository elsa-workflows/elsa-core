using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Bridged", "Triggered when an a call is bridged.")]
    public sealed record CallBridgedPayload : CallPayload
    {
        public const string EventType = "call.bridged";
        public const string ActivityTypeName = "CallBridged";
        public string From { get; init; } = default!;
        public string To { get; init; } = default!;
        public string State { get; init; } = default!;
    }
}