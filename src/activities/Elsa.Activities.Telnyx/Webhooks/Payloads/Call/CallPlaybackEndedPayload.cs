using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Playback Ended", "Triggered when an audio playback has ended.")]
    public sealed record CallPlaybackEndedPayload : CallPlayback
    {
        public const string EventType = "call.playback.ended";
        public const string ActivityTypeName = "CallPlaybackEnded";
        public string Status { get; set; } = default!;
    }
}