using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Payload(EventType, "TelnyxCallInitiated", "Call Initiated", "Triggered when an incoming call is received.")]
    public sealed record CallInitiatedPayload : CallPayload
    {
        public const string EventType = "call.initiated";
        public string Direction { get; init; } = default!;
        public string State { get; init; } = default!;
    }
}