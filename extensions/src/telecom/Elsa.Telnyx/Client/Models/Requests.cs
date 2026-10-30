namespace Elsa.Telnyx.Client.Models;

public record AnswerCallRequest(
    string? BillingGroupId = null,
    string? ClientState = null,
    string? CommandId = null,
    string? WebhookUrl = null,
    string? WebhookUrlMethod = null);

public record HangupCallRequest(string? ClientState = null, string? CommandId = null);

public record GatherUsingAudioRequest(
    Uri AudioUrl,
    string? ClientState = null,
    string? CommandId = null,
    int? InterDigitTimeoutMillis = null,
    Uri? InvalidAudioUrl = null,
    int? MaximumDigits = null,
    int? MaximumTries = null,
    int? MinimumDigits = null,
    string? TerminatingDigit = null,
    int? TimeoutMillis = null,
    string? ValidDigits = null
);

public record GatherUsingSpeakRequest(
    string Language,
    string Voice,
    string Payload,
    string? PayloadType = null,
    string? ServiceLevel = null,
    int? InterDigitTimeoutMillis = null,
    int? MaximumDigits = null,
    int? MaximumTries = null,
    int? MinimumDigits = null,
    string? TerminatingDigit = null,
    int? TimeoutMillis = null,
    string? ValidDigits = null,
    string? ClientState = null,
    string? CommandId = null
);

public record TransferCallRequest(
    string To,
    string? From = null,
    string? FromDisplayName = null,
    Uri? AudioUrl = null,
    string? AnsweringMachineDetection = null,
    AnsweringMachineConfig? AnsweringMachineConfig = null,
    int? TimeLimitSecs = null,
    int? TimeoutSecs = null,
    string? TargetLegClientState = null,
    IList<Header>? CustomHeaders = null,
    string? SipAuthUsername = null,
    string? SipAuthPassword = null,
    string? ClientState = null,
    string? CommandId = null,
    string? WebhookUrl = null,
    string? WebhookUrlMethod = null
);

public record DialRequest(
    string ConnectionId,
    string To,
    string? From = null,
    string? FromDisplayName = null,
    string? AnsweringMachineDetection = null,
    AnsweringMachineConfig? AnsweringMachineConfig = null,
    string? Record = null,
    string? RecordFormat = null,
    string? ClientState = null,
    string? CommandId = null,
    IList<Header>? CustomHeaders = null,
    string? SipAuthUsername = null,
    string? SipAuthPassword = null,
    int? TimeLimitSecs = null,
    int? TimeoutSecs = null,
    string? WebhookUrl = null,
    string? WebhookUrlMethod = null
);

public record BridgeCallsRequest(
    string CallControlId,
    string? ClientState = null,
    string? CommandId = null,
    string? ParkAfterUnbridge = null
);

public record PlayAudioRequest(
    Uri AudioUrl,
    bool Overlay,
    object? Loop = null,
    string? TargetLegs = null,
    string? ClientState = null,
    string? CommandId = null
);

public record StopAudioPlaybackRequest(
    string? Stop = null,
    string? ClientState = null,
    string? CommandId = null
);

public record StartRecordingRequest(
    string Channels,
    string Format,
    bool? PlayBeep = null,
    string? ClientState = null,
    string? CommandId = null
);

public record StopRecordingRequest(
    string? ClientState = null,
    string? CommandId = null
);

public record SpeakTextRequest(
    string Language,
    string Voice,
    string Payload,
    string? PayloadType = null,
    string? ServiceLevel = null,
    string? Stop = null,
    string? ClientState = null,
    string? CommandId = null
);