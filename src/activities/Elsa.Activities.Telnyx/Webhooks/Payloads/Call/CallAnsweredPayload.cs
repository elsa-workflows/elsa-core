using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Answered", "Triggered when an incoming call is answered.")]
    public sealed record CallAnsweredPayload : CallPayload
    {
        public const string EventType = "call.answered";
        public const string ActivityTypeName = "CallAnswered";
        public string From { get; init; } = default!;
        public string To { get; init; } = default!;
        public string State { get; init; } = default!;
    }
}