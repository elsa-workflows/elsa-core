using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallHangup, WebhookActivityTypeNames.CallHangup, "Call Hangup", "Triggered when an incoming call was hangup.")]
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