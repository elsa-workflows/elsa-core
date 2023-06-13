namespace Elsa.Activities.Telnyx.Client.Models;

public record StartTranscriptionRequest(
    string TranscriptionTracks,
    string Language,
    string? ClientState,
    string? CommandId,
    bool? InterimResults
);