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
    public string SipHangupCause { get; init; } = null!;
    public string HangupSource { get; init; } = null!;
    public string HangupCause { get; init; } = null!;
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
}