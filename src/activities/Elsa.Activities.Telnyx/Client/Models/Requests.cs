using System;
using System.Collections.Generic;

namespace Elsa.Activities.Telnyx.Client.Models
{
    public record AnswerCallRequest(string? BillingGroupId, string? ClientState, string? CommandId, string? WebhookUrl, string? WebhookUrlMethod);
    public record HangupCallRequest(string? ClientState, string? CommandId);
    
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
    
    public record TransferCallRequest(
        string To,
        string? From,
        string? FromDisplayName,
        Uri? AudioUrl,
        string? AnsweringMachineDetection,
        AnsweringMachineConfig? AnsweringMachineConfig,
        string? ClientState,
        string? TargetLegClientState,
        string? CommandId,
        IList<Header>? CustomHeaders,
        string? SipAuthUsername,
        string? SipAuthPassword,
        int? TimeLimitSecs,
        int? TimeoutSecs,
        string? WebhookUrl,
        string? WebhookUrlMethod
    );
}