using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Gather Ended", "Triggered when an call gather has ended.")]
    public sealed record CallGatherEndedPayload : CallPayload
    {
        public const string EventType = "call.gather.ended";
        public const string ActivityTypeName = "CallGatherEnded";
        public string Digits { get; set; } = default!;
        public string From { get; set; } = default!;
        public string To { get; set; } = default!;
        public string Status { get; set; } = default!;
    }
}