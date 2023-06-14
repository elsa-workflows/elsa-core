using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Transcription Received", "Triggered when a call transcription update was received.")]
    public sealed record CallTranscriptionReceived : CallPayload
    {
        public const string EventType = "call.transcription";
        public const string ActivityTypeName = "CallTranscriptionReceived";
        public CallRecordingUrls PublicRecordingUrls { get; set; } = default!;
        public CallRecordingUrls RecordingUrls { get; set; } = default!;
        public TranscriptionData TranscriptionData { get; set; } = default!;
    }
}