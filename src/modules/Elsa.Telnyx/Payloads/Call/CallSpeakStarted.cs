using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallSpeakStarted)]
public sealed record CallSpeakStarted : CallPayload;