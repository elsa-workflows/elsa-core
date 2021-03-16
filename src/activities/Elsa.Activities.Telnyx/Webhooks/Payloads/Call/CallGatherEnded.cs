using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Payload(EventType, "TelnyxCallGatherEnded", "Call Gather Ended", "Triggered when an call gather has ended.")]
    public sealed record CallGatherEnded : CallPayload
    {
        public const string EventType = "call.gather.ended";
        public string Digits { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Status { get; set; }
    }
}