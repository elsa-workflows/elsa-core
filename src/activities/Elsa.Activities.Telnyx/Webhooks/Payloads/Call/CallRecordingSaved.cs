using Elsa.Activities.Telnyx.Webhooks.Attributes;
using NodaTime;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Recording Saved", "Triggered when a recording has been saved.")]
    public sealed record CallRecordingSaved : CallPayload
    {
        public const string EventType = "call.recording.saved";
        public const string ActivityTypeName = "CallRecordingSaved";
        public string Channels { get; set; } = default!;
        public CallRecordingUrls PublicRecordingUrls { get; set; } = default!;
        public CallRecordingUrls RecordingUrls { get; set; } = default!;
        public Instant RecordingEndedAt { get; set; }
        public Instant RecordingStartedAt { get; set; }
        public Duration Duration => RecordingEndedAt - RecordingStartedAt;
    }
}