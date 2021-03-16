using System;

namespace Elsa.Activities.Telnyx.Client.Models
{
    public record GatherUsingAudioRequest(
        Uri AudioUrl,
        string? ClientState,
        string? CommandId,
        int? InterDigitTimeoutMillis,
        Uri? InvalidAudioUrl,
        int? MaximumDigits,
        int? MaximumTries,
        int? MinimumDigits,
        string? TerminatingDigit,
        int? TimeoutMillis,
        string? ValidDigits);
}