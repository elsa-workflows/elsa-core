using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallRecordingSaved, ActivityTypeName, "Call Recording Saved", "Triggered when a recording has been saved.")]
public sealed record CallRecordingSaved : CallPayload
{
    public const string ActivityTypeName = "CallRecordingSaved";
    public string Channels { get; set; } = default!;
    public CallRecordingUrls PublicRecordingUrls { get; set; } = default!;
    public CallRecordingUrls RecordingUrls { get; set; } = default!;
    public DateTimeOffset RecordingEndedAt { get; set; }
    public DateTimeOffset RecordingStartedAt { get; set; }
    public TimeSpan Duration => RecordingEndedAt - RecordingStartedAt;
}