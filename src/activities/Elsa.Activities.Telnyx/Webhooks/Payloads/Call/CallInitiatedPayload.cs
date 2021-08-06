using Elsa.Activities.Telnyx.Webhooks.Attributes;
using NodaTime;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Initiated", "Triggered when an incoming call is received.")]
    public sealed record CallInitiatedPayload : CallPayload
    {
        public const string EventType = "call.initiated";
        public const string ActivityTypeName = "CallInitiated";
        public string Direction { get; init; } = default!;
        public string State { get; init; } = default!;
        public string To { get; init; } = default!;
        public string From { get; init; } = default!;
        public Instant StartTime { get; set; } = default!;
    }
}