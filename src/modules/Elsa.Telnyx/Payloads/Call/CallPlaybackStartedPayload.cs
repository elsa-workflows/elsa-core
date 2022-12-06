using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallPlaybackStarted, WebhookActivityTypeNames.CallPlaybackStarted, "Call Playback Started", "Triggered when an audio playback has started.")]
public sealed record CallPlaybackStartedPayload : CallPlayback;