using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Admission;

/// <summary>Durable admission phases. ExecutionObserved is not necessarily terminal.</summary>
public enum AdmissionState { Admitted, Creating, Materialized, StartPreparing, StartAuthorized, ExecutionObserved, RecoveryRequired, Terminal }
/// <summary>Qualified terminal outcomes, never inferred from a lease or a runner returning.</summary>
public enum AdmissionTerminalDisposition { Completed, SuppressedBeforeStart, Resolved }
/// <summary>Explicit handling of input outside the accepted trusted time window.</summary>
public enum AdmissionRejectedEventDisposition { Reject, Quarantine }
/// <summary>Admission outcomes. Only Committed and Duplicate establish acknowledgement eligibility.</summary>
public enum AdmissionOutcome { Committed, Duplicate, Filtered, Inactive, CapacityExceeded, Rejected, Quarantined }

/// <summary>No implicit production policy. Every value must be supplied and validated before activation.</summary>
public sealed record AdmissionPolicy(
    TimeSpan PayloadRetention,
    TimeSpan IdentityHorizon,
    TimeSpan MaximumEventAge,
    TimeSpan MaximumClockSkew,
    int ActiveCapacity,
    int RetainedRecordCapacity,
    AdmissionRejectedEventDisposition LateEventDisposition,
    AdmissionRejectedEventDisposition InvalidEventDisposition,
    string CleanupAuthority)
{
    public void Validate()
    {
        if (PayloadRetention < TimeSpan.Zero || MaximumEventAge <= TimeSpan.Zero || MaximumClockSkew < TimeSpan.Zero ||
            IdentityHorizon < MaximumEventAge + MaximumClockSkew || IdentityHorizon < PayloadRetention ||
            ActiveCapacity <= 0 || RetainedRecordCapacity < ActiveCapacity || string.IsNullOrWhiteSpace(CleanupAuthority) ||
            !Enum.IsDefined(LateEventDisposition) || !Enum.IsDefined(InvalidEventDisposition))
        {
            throw new ArgumentException("Explicit admission retention, time, capacity and cleanup policy is invalid.");
        }
    }
}

/// <summary>Trusted configuration; installation identity is independent of credential/token/reconnect generations.</summary>
public sealed record AdmissionSubscriptionConfiguration(
    string Id, string TenantId, string EnvironmentId, string InstallationId, string ChannelId,
    string DefinitionId, string DefinitionVersionId, int DefinitionVersion, string DefinitionFingerprint,
    DateTimeOffset ActivationBoundary, AdmissionPolicy Policy)
{
    public void Validate()
    {
        Policy.Validate();
        foreach (var value in new[] { Id, TenantId, EnvironmentId, InstallationId, ChannelId, DefinitionId, DefinitionVersionId, DefinitionFingerprint })
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
            {
                throw new ArgumentException("Admission bindings must be explicit and bounded.");
            }
        }
        if (DefinitionVersion <= 0 || DefinitionFingerprint.Length != 64 || !DefinitionFingerprint.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("A pinned definition version and SHA-256 fingerprint are required.");
        }
    }

    [JsonIgnore]
    public string ConfigurationFingerprint => AdmissionHash.Compute(JsonSerializer.Serialize(this));
}

/// <summary>Transport-independent synthetic/provider event. Envelope identity is deliberately absent.</summary>
public sealed record AdmissionEvent(string SubscriptionId, string InstallationId, string ChannelId, string ProviderEventId,
    DateTimeOffset? OccurredAt, bool IsHumanMessage, bool IsLoopMessage, string Payload);

/// <summary>Persisted configuration and separate active/retained capacity counters.</summary>
public sealed class AdmissionSubscription
{
    public string Id { get; set; } = null!;
    public string ConfigurationJson { get; set; } = null!;
    public string ConfigurationFingerprint { get; set; } = null!;
    public long Revision { get; set; }
    public bool Active { get; set; }
    public bool Retired { get; set; }
    public bool BootstrapVerified { get; set; }
    public int ActiveReservations { get; set; }
    public int RetainedRecords { get; set; }
    public string? ReconciliationCode { get; set; }
    public AdmissionSubscriptionConfiguration Configuration => JsonSerializer.Deserialize<AdmissionSubscriptionConfiguration>(ConfigurationJson)!;
}

/// <summary>
/// Durable identity/state and execution ownership. Cleanup erases identity/payload, not instance ownership.
/// This record contains no invocable capability; StartAuthorized is only a historical durable fact.
/// </summary>
public sealed class AdmissionRecord
{
    public string Id { get; set; } = null!;
    public string SubscriptionId { get; set; } = null!;
    public string? IdentityHash { get; set; }
    public string? ProviderEventId { get; set; }
    public string? Payload { get; set; }
    public string ConfigurationFingerprint { get; set; } = null!;
    public DateTimeOffset AdmittedAt { get; set; }
    public DateTimeOffset EventOccurredAt { get; set; }
    public long Revision { get; set; }
    public AdmissionState State { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? AttemptId { get; set; }
    public bool AuthorityOutstanding { get; set; }
    public string? CheckpointFingerprint { get; set; }
    public string? BookmarkIdsJson { get; set; }
    public string? RecoveryCode { get; set; }
    public AdmissionTerminalDisposition? TerminalDisposition { get; set; }
    public DateTimeOffset? TerminalAt { get; set; }
    public bool ActiveReservationReleased { get; set; }
    public bool RetainedRecordReleased { get; set; }
}

/// <summary>Sanitized outcome. Payload, event content and credential values are never exported here.</summary>
public sealed record AdmissionResult(AdmissionOutcome Outcome, string? AdmissionId, long? Revision)
{
    public bool AcknowledgementEligible => Outcome is AdmissionOutcome.Committed or AdmissionOutcome.Duplicate;
}

/// <summary>Immutable digest utilities used for trusted configuration/identity, not executable type deserialization.</summary>
public static class AdmissionHash
{
    public static string Compute(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public static string Identity(AdmissionSubscriptionConfiguration configuration, string eventId) =>
        Compute(JsonSerializer.Serialize(new[] { configuration.TenantId, configuration.EnvironmentId, configuration.InstallationId, configuration.Id, eventId }));
}
