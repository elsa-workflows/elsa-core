using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallSpeakEnded, ActivityTypeName, "Call Speak Ended", "Triggered when speaking has ended.")]
public sealed record CallSpeakEnded : CallPayload
{
    public const string ActivityTypeName = "CallSpeakEnded";
}