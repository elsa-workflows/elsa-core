using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call DTMF Received", "Triggered when DTMF input is received.")]
    public sealed record CallDtmfReceivedPayload : CallPayload
    {
        public const string EventType = "call.dtmf.received";
        public const string ActivityTypeName = "CallDtmfReceived";
        public string Digit { get; set; } = default!;
        public string From { get; set; } = default!;
        public string To { get; set; } = default!;
    }
}