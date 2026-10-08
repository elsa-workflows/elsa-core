using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

[WebhookActivity(WebhookEventTypes.CallPlaybackStarted, WebhookActivityTypeNames.CallPlaybackStarted, "Call Playback Started", "Triggered when an audio playback has started.")]
public sealed record CallPlaybackStartedPayload : CallPlayback;