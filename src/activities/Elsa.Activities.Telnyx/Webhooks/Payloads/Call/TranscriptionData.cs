namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    public record TranscriptionData(double Confidence, bool IsFinal, string Transcript);
}