using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallPlaybackEnded, WebhookActivityTypeNames.CallPlaybackEnded, "Call Playback Ended", "Triggered when an audio playback has ended.")]
public sealed record CallPlaybackEndedPayload : CallPlayback
{
    public string Status { get; set; } = default!;
}