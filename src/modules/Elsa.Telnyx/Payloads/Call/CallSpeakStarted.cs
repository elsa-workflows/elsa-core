using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallSpeakStarted)]
public sealed record CallSpeakStarted : CallPayload;