using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallBridged, ActivityTypeName, "Call Bridged", "Triggered when an a call is bridged.")]
public sealed record CallBridgedPayload : CallPayload
{
    public const string ActivityTypeName = "CallBridged";
    public string From { get; init; } = default!;
    public string To { get; init; } = default!;
    public string State { get; init; } = default!;
}