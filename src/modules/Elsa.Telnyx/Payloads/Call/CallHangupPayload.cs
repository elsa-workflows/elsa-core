using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

/// <summary>
/// A payload representing the call.hangup Telnyx webhook event.
/// </summary>
[Webhook(WebhookEventTypes.CallHangup)]
public sealed record CallHangupPayload : CallPayload
{
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public string SipHangupCause { get; init; } = default!;
    public string HangupSource { get; init; } = default!;
    public string HangupCause { get; init; } = default!;
    public string From { get; set; } = default!;
    public string To { get; set; } = default!;
}