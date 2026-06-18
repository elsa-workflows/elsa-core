using System.Text.Json.Serialization;

namespace Elsa.Platform.Integration.Models;

[JsonConverter(typeof(JsonStringEnumConverter<PlatformRuntimeCommandAction>))]
public enum PlatformRuntimeCommandAction
{
    Unknown,
    Deploy,
    Rollback,
    Validate,
    RuntimeControl
}

[JsonConverter(typeof(JsonStringEnumConverter<PlatformRuntimeCommandStatus>))]
public enum PlatformRuntimeCommandStatus
{
    Unknown,
    Pending,
    Claimed,
    Running,
    Completed,
    Failed,
    Rejected,
    Cancelled,
    RecoveryRequired,
    Expired
}

[JsonConverter(typeof(JsonStringEnumConverter<PlatformArtifactStatus>))]
public enum PlatformArtifactStatus
{
    Pending,
    Downloading,
    Validated,
    Applying,
    Applied,
    Failed,
    Rejected,
    Skipped
}

[JsonConverter(typeof(JsonStringEnumConverter<PlatformDiagnosticSeverity>))]
public enum PlatformDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record PlatformRuntimeCommandListResponse(IReadOnlyList<PlatformRuntimeCommand> Commands);

public sealed record PlatformRuntimeCommandClaimRequest(Guid EngineId, string WorkerId, int LeaseSeconds);

public sealed record PlatformRuntimeCommandClaimResponse(PlatformRuntimeCommand Command, string LeaseToken);

public sealed record PlatformRuntimeCommandHeartbeatRequest(string LeaseToken, string WorkerId);

public sealed record PlatformRuntimeCommandProgressRequest(string LeaseToken, string Status, int? PercentComplete, string Message);

public sealed record PlatformRuntimeCommandCompleteRequest(
    string LeaseToken,
    PlatformArtifactDigest? ObservedArtifactDigest,
    string? RuntimeReference,
    IReadOnlyList<PlatformDiagnostic> Diagnostics,
    IReadOnlyList<PlatformArtifactOutcome>? Artifacts = null);

public sealed record PlatformRuntimeCommandFailRequest(
    string LeaseToken,
    IReadOnlyList<PlatformDiagnostic> Diagnostics,
    IReadOnlyList<PlatformArtifactOutcome>? Artifacts = null);

public sealed record PlatformRuntimeCommandRejectRequest(
    string LeaseToken,
    IReadOnlyList<PlatformDiagnostic> Diagnostics,
    IReadOnlyList<PlatformArtifactOutcome>? Artifacts = null);

public sealed record PlatformRuntimeCommand(
    Guid Id,
    Guid WorkspaceId,
    Guid RunId,
    Guid EnvironmentId,
    Guid EngineId,
    PlatformRuntimeCommandAction Action,
    PlatformRuntimeCommandStatus Status,
    PlatformRuntimeCommandArtifactReference? Artifact,
    PlatformRuntimeCommandRevisionReference? Revision,
    string IdempotencyKey,
    string? WorkerId,
    DateTimeOffset? ClaimedAt,
    DateTimeOffset? LeaseExpiresAt,
    DateTimeOffset? HeartbeatAt,
    int AttemptNumber,
    int? PercentComplete,
    string? ProgressMessage,
    PlatformArtifactDigest? ObservedArtifactDigest,
    string? RuntimeReference,
    IReadOnlyList<PlatformDiagnostic> Diagnostics,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? AvailableAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<PlatformArtifactItem>? Artifacts = null);

public sealed record PlatformRuntimeCommandArtifactReference(
    Guid? ArtifactRecordId,
    string? ArtifactId,
    string? ArtifactTypeId,
    PlatformArtifactDigest? ContentDigest);

public sealed record PlatformRuntimeCommandRevisionReference(Guid? RevisionId);

public sealed record PlatformArtifactItem(
    Guid ArtifactRecordId,
    string ArtifactId,
    string ArtifactTypeId,
    string? ArtifactSchemaVersion,
    PlatformArtifactDigest ContentDigest,
    string DisplayName,
    string? DownloadUrl,
    PlatformArtifactStatus Status,
    PlatformArtifactDigest? ObservedDigest,
    string? RuntimeReference,
    IReadOnlyList<PlatformDiagnostic>? Diagnostics);

public sealed record PlatformArtifactOutcome(
    Guid ArtifactRecordId,
    PlatformArtifactStatus Status,
    PlatformArtifactDigest? ObservedDigest = null,
    string? RuntimeReference = null,
    IReadOnlyList<PlatformDiagnostic>? Diagnostics = null);

public sealed record PlatformArtifactDigest(string Algorithm, string Value);

public sealed record PlatformDiagnostic(string Code, PlatformDiagnosticSeverity Severity, string Message);

public sealed record PlatformProblemDetails(string? Title, string? Detail);
