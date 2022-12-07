using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallSpeakStarted, WebhookActivityTypeNames.CallSpeakStarted, "Call Speak Started", "Triggered when speaking has started.")]
public sealed record CallSpeakStarted : CallPayload;