using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallPlaybackStarted, ActivityTypeName, "Call Playback Started", "Triggered when an audio playback has started.")]
public sealed record CallPlaybackStartedPayload : CallPlayback
{
    public const string ActivityTypeName = "CallPlaybackStarted";
}