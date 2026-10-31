namespace Elsa.Studio.PlatformIntegration;

/// <summary>
/// Represents a Platform submission result.
/// </summary>
public sealed record PlatformSubmitResult(
    PlatformSubmitStatus Status,
    string Message,
    string? ArtifactId = null,
    string? ArtifactDigest = null,
    DateTimeOffset? RegisteredAt = null,
    Guid? ArtifactRecordId = null,
    Guid? RevisionId = null)
{
    /// <summary>
    /// Whether the submission succeeded.
    /// </summary>
    public bool Succeeded => Status is PlatformSubmitStatus.Submitted or PlatformSubmitStatus.Duplicate;
}

/// <summary>
/// Represents Platform submission status.
/// </summary>
public enum PlatformSubmitStatus
{
    /// <summary>
    /// Artifact was newly submitted.
    /// </summary>
    Submitted,

    /// <summary>
    /// Artifact already existed in Platform.
    /// </summary>
    Duplicate,

    /// <summary>
    /// Platform rejected the submission because Studio is not authorized.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Platform rejected the submission because of a state conflict.
    /// </summary>
    Conflict,

    /// <summary>
    /// Platform rejected the submission because the request was invalid.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// Platform submission failed in a way that can be retried.
    /// </summary>
    RetryableError
}
