using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Payload(EventType, "TelnyxCallDtmfReceived", "Call DTMF Received", "Triggered when DTMF input is received.")]
    public sealed record CallDtmfReceived : CallPayload
    {
        public const string EventType = "call.dtmf.received";
        public string Digit { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}