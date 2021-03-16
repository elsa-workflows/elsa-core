using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Payload(EventType, "TelnyxCallPlaybackEnded", "Call Playback Ended", "Triggered when an audio playback has ended.")]
    public sealed record CallPlaybackEnded : CallPlayback
    {
        public const string EventType = "call.playback.ended";
        public string Status { get; set; }
    }
}