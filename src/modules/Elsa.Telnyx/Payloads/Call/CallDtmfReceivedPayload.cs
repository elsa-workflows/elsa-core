using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call
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