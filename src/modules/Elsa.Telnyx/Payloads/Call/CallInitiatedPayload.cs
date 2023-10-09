using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallInitiated)]
public sealed record CallInitiatedPayload : CallPayload
{ 
    public string Direction { get; init; } = default!;
    public string State { get; init; } = default!;
    public string To { get; init; } = default!;
    public string From { get; init; } = default!;
    public DateTimeOffset StartTime { get; set; } = default!;
}