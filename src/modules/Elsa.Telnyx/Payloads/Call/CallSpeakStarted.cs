using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallSpeakStarted, ActivityTypeName, "Call Speak Started", "Triggered when speaking has started.")]
public sealed record CallSpeakStarted : CallPayload
{
    public const string ActivityTypeName = "CallSpeakStarted";
}