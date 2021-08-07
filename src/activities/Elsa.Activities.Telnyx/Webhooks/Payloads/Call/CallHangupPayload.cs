using Elsa.Activities.Telnyx.Webhooks.Attributes;
using NodaTime;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Hangup", "Triggered when an incoming call was hangup.")]
    public sealed record CallHangupPayload : CallPayload
    {
        public const string EventType = "call.hangup";
        public const string ActivityTypeName = "CallHangup";
        public Instant StartTime { get; init; }
        public Instant EndTime { get; init; }
        public string SipHangupCause { get; init; } = default!;
        public string HangupSource { get; init; } = default!;
        public string HangupCause { get; init; } = default!;
        public string From { get; set; } = default!;
        public string To { get; set; } = default!;
    }
}