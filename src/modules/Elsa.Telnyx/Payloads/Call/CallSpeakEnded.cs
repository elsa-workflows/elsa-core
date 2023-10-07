using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallSpeakEnded)]
public sealed record CallSpeakEnded : CallPayload;