using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

[WebhookActivity(WebhookEventTypes.CallGatherEnded, WebhookActivityTypeNames.CallGatherEnded, "Call Gather Ended", "Triggered when an call gather has ended.")]
public sealed record CallGatherEndedPayload : CallPayload
{
    public string Digits { get; set; } = default!;
    public string From { get; set; } = default!;
    public string To { get; set; } = default!;
    public string Status { get; set; } = default!;
}