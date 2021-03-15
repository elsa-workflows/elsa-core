using Elsa.Activities.Telnyx.Attributes;
using Elsa.Activities.Telnyx.Payloads.Abstract;
using NodaTime;

namespace Elsa.Activities.Telnyx.Payloads
{
    [Payload(EventType, "TelnyxCallHangup", "Call Hangup", "Triggered when an incoming call was hangup.")]
    public sealed class CallHangupPayload : CallPayload
    {
        public const string EventType = "call.hangup";
        public Instant StartTime { get; set; }
        public Instant EndTime { get; set; }
        public string SipHangupCause { get; set; } = default!;
        public string HangupSource { get; set; } = default!;
        public string HangupCause { get; set; } = default!;
    }
}