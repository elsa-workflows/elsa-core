using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Payload(EventType, "TelnyxCallPlaybackStarted", "Call Playback Started", "Triggered when an audio playback has started.")]
    public sealed record CallPlaybackStarted : CallPlayback
    {
        public const string EventType = "call.playback.started";
    }
}