using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallPlaybackEnded, ActivityTypeName, "Call Playback Ended", "Triggered when an audio playback has ended.")]
public sealed record CallPlaybackEndedPayload : CallPlayback
{
    public const string ActivityTypeName = "CallPlaybackEnded";
    public string Status { get; set; } = default!;
}