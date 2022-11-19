using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallAnswered, ActivityTypeName, "Call Answered", "Triggered when an incoming call is answered.")]
public sealed record CallAnsweredPayload : CallPayload
{
    public const string ActivityTypeName = "CallAnswered";
    public string From { get; init; } = default!;
    public string To { get; init; } = default!;
    public string State { get; init; } = default!;
}