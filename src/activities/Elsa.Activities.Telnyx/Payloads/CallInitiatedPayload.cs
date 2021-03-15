using Elsa.Activities.Telnyx.Attributes;
using Elsa.Activities.Telnyx.Payloads.Abstract;

namespace Elsa.Activities.Telnyx.Payloads
{
    [Payload(EventType, "TelnyxCallInitiated", "Call Initiated", "Triggered when an incoming call is received.")]
    public sealed class CallInitiatedPayload : CallPayload
    {
        public const string EventType = "call.initiated";
        public string Direction { get; set; } = default!;
        public string State { get; set; } = default!;
    }
}