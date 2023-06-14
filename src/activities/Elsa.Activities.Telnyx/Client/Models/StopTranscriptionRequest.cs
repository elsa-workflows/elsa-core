namespace Elsa.Activities.Telnyx.Client.Models;

public record StopTranscriptionRequest(
    string? ClientState,
    string? CommandId
);