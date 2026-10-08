using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallInitiated)]
public sealed record CallInitiatedPayload : CallPayload
{ 
    public string Direction { get; init; } = null!;
    public string State { get; init; } = null!;
    public string To { get; init; } = null!;
    public string From { get; init; } = null!;
    public DateTimeOffset StartTime { get; set; } = default!;
}