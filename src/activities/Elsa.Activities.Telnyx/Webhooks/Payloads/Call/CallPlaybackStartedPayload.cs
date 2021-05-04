using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Playback Started", "Triggered when an audio playback has started.")]
    public sealed record CallPlaybackStartedPayload : CallPlayback
    {
        public const string EventType = "call.playback.started";
        public const string ActivityTypeName = "CallPlaybackStarted";
    }
}