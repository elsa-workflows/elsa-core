using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Payloads.Call;

[WebhookActivity(WebhookEventTypes.CallPlaybackEnded, WebhookActivityTypeNames.CallPlaybackEnded, "Call Playback Ended", "Triggered when an audio playback has ended.")]
public sealed record CallPlaybackEndedPayload : CallPlayback
{
    public string Status { get; set; } = default!;
}