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
        string? ValidDigits
    );

    public record GatherUsingSpeakRequest(
        string? ClientState,
        string? CommandId,
        string Language,
        string Voice,
        string Payload,
        string? PayloadType,
        string? ServiceLevel,
        int? InterDigitTimeoutMillis,
        int? MaximumDigits,
        int? MaximumTries,
        int? MinimumDigits,
        string? TerminatingDigit,
        int? TimeoutMillis,
        string? ValidDigits
    );

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

    public record DialRequest(
        string ConnectionId,
        string To,
        string? From,
        string? FromDisplayName,
        string? AnsweringMachineDetection,
        AnsweringMachineConfig? AnsweringMachineConfig,
        string? ClientState,
        string? CommandId,
        IList<Header>? CustomHeaders,
        string? SipAuthUsername,
        string? SipAuthPassword,
        string? Record,
        string? RecordFormat,
        int? TimeLimitSecs,
        int? TimeoutSecs,
        string? WebhookUrl,
        string? WebhookUrlMethod
    );

    public record BridgeCallsRequest(
        string CallControlId,
        string? ClientState,
        string? CommandId,
        string? ParkAfterUnbridge
    );

    public record PlayAudioRequest(
        Uri AudioUrl,
        string? ClientState,
        string? CommandId,
        object? Loop,
        bool Overlay,
        string? TargetLegs
    );
    
    public record StopAudioPlaybackRequest(
        string? ClientState,
        string? CommandId,
        string? Stop
    );

    public record StartRecordingRequest(
        string Channels,
        string Format,
        string? ClientState,
        string? CommandId,
        bool? PlayBeep
    );

    public record StopRecordingRequest(
        string? ClientState,
        string? CommandId
    );

    public record SpeakTextRequest(
        string Language,
        string Voice,
        string Payload,
        string? PayloadType,
        string? ServiceLevel,
        string? ClientState,
        string? CommandId,
        string? Stop
    );
}