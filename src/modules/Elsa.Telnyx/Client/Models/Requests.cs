namespace Elsa.Telnyx.Client.Models;

public record AnswerCallRequest(
    string? BillingGroupId = default,
    string? ClientState = default,
    string? CommandId = default,
    string? WebhookUrl = default,
    string? WebhookUrlMethod = default);

public record HangupCallRequest(string? ClientState = default, string? CommandId = default);

public record GatherUsingAudioRequest(
    Uri AudioUrl,
    string? ClientState = default,
    string? CommandId = default,
    int? InterDigitTimeoutMillis = default,
    Uri? InvalidAudioUrl = default,
    int? MaximumDigits = default,
    int? MaximumTries = default,
    int? MinimumDigits = default,
    string? TerminatingDigit = default,
    int? TimeoutMillis = default,
    string? ValidDigits = default
);

public record GatherUsingSpeakRequest(
    string Language,
    string Voice,
    string Payload,
    string? PayloadType = default,
    string? ServiceLevel = default,
    int? InterDigitTimeoutMillis = default,
    int? MaximumDigits = default,
    int? MaximumTries = default,
    int? MinimumDigits = default,
    string? TerminatingDigit = default,
    int? TimeoutMillis = default,
    string? ValidDigits = default,
    string? ClientState = default,
    string? CommandId = default
);

public record TransferCallRequest(
    string To,
    string? From = default,
    string? FromDisplayName = default,
    Uri? AudioUrl = default,
    string? AnsweringMachineDetection = default,
    AnsweringMachineConfig? AnsweringMachineConfig = default,
    int? TimeLimitSecs = default,
    int? TimeoutSecs = default,
    string? TargetLegClientState = default,
    IList<Header>? CustomHeaders = default,
    string? SipAuthUsername = default,
    string? SipAuthPassword = default,
    string? ClientState = default,
    string? CommandId = default,
    string? WebhookUrl = default,
    string? WebhookUrlMethod = default
);

public record DialRequest(
    string ConnectionId,
    string To,
    string? From = default,
    string? FromDisplayName = default,
    string? AnsweringMachineDetection = default,
    AnsweringMachineConfig? AnsweringMachineConfig = default,
    string? Record = default,
    string? RecordFormat = default,
    string? ClientState = default,
    string? CommandId = default,
    IList<Header>? CustomHeaders = default,
    string? SipAuthUsername = default,
    string? SipAuthPassword = default,
    int? TimeLimitSecs = default,
    int? TimeoutSecs = default,
    string? WebhookUrl = default,
    string? WebhookUrlMethod = default
);

public record BridgeCallsRequest(
    string CallControlId,
    string? ClientState = default,
    string? CommandId = default,
    string? ParkAfterUnbridge = default
);

public record PlayAudioRequest(
    Uri AudioUrl,
    bool Overlay,
    object? Loop = default,
    string? TargetLegs = default,
    string? ClientState = default,
    string? CommandId = default
);

public record StopAudioPlaybackRequest(
    string? Stop = default,
    string? ClientState = default,
    string? CommandId = default
);

public record StartRecordingRequest(
    string Channels,
    string Format,
    bool? PlayBeep = default,
    string? ClientState = default,
    string? CommandId = default
);

public record StopRecordingRequest(
    string? ClientState = default,
    string? CommandId = default
);

public record SpeakTextRequest(
    string Language,
    string Voice,
    string Payload,
    string? PayloadType = default,
    string? ServiceLevel = default,
    string? Stop = default,
    string? ClientState = default,
    string? CommandId = default
);