using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Payload(EventType, ActivityTypeName, "Call Gather Ended", "Triggered when an call gather has ended.")]
    public sealed record CallGatherEndedPayload : CallPayload
    {
        public const string EventType = "call.gather.ended";
        public const string ActivityTypeName = "CallGatherEnded";
        public string Digits { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Status { get; set; }
    }
}