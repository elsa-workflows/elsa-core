using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Studio.PlatformIntegration;

[JsonConverter(typeof(JsonStringEnumConverter<PlatformDeploymentArtifactEntryKind>))]
internal enum PlatformDeploymentArtifactEntryKind
{
    Metadata,
    Manifest,
    ChecksumInventory,
    Payload
}

internal sealed record PlatformDeploymentArtifactManifestMetadata(
    string? Name,
    string? Version,
    string? Environment,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<string, string> Annotations);

internal sealed record PlatformDeploymentArtifactResourceSummary(
    string Type,
    string LogicalId,
    string? Scope,
    string? Version,
    PlatformArtifactDigest? DesiredStateHash);

internal sealed record PlatformDeploymentArtifactMetadata(
    string LayoutVersion,
    string ArtifactId,
    DateTimeOffset CreatedAt,
    PlatformDeploymentArtifactManifestMetadata Manifest,
    IReadOnlyCollection<PlatformDeploymentArtifactResourceSummary> Resources,
    PlatformArtifactDigest ContentDigest,
    string? Builder = null,
    string? Source = null);

internal sealed record PlatformDeploymentArtifactChecksumEntry(
    string Path,
    PlatformDeploymentArtifactEntryKind Kind,
    string Algorithm,
    string Digest,
    long Size);

internal sealed record PlatformDeploymentArtifactChecksumInventory(
    string Algorithm,
    IReadOnlyCollection<PlatformDeploymentArtifactChecksumEntry> Entries);

internal sealed record PlatformEnvironmentManifest(
    string ApiVersion,
    string Kind,
    PlatformManifestMetadata Metadata,
    PlatformManifestResources Resources);

internal sealed record PlatformManifestMetadata(
    string? Name,
    string? Version,
    string? Environment,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<string, string> Annotations);

internal sealed record PlatformManifestResources(IReadOnlyList<PlatformRecipeManifestEntry> Recipes);

internal sealed record PlatformRecipeManifestEntry(
    string Id,
    string Path,
    string? Version,
    IReadOnlyList<object> Dependencies,
    IReadOnlyDictionary<string, string> Metadata);

internal sealed record CreateArtifactUploadRequest(
    string FileName,
    string? ContentType,
    long SizeBytes,
    string? IdempotencyKey = null);

internal sealed record CreateArtifactUploadResponse(
    Guid UploadId,
    string Status,
    DateTimeOffset ExpiresAt,
    long MaxUploadBytes);

internal sealed record CompleteArtifactUploadResponse(
    Guid UploadId,
    string Status,
    WorkspaceArtifactResponse? Artifact,
    bool Created,
    IReadOnlyList<WorkspaceArtifactDiagnostic> Diagnostics);

internal sealed record WorkspaceDesiredStateRevisionRequest(
    string Label,
    string? Commit,
    IReadOnlyList<WorkspaceDesiredStateRecordRequest> Records);

internal sealed record WorkspaceDesiredStateRecordRequest(
    string Kind,
    string Name,
    JsonElement Payload);

internal sealed record ArtifactReferencePayload(
    Guid ArtifactRecordId,
    string ArtifactId,
    string ArtifactTypeId,
    PlatformArtifactDigest ContentDigest);

internal sealed record WorkspaceDesiredStateRevisionResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ApplicationId,
    Guid EnvironmentId,
    string Label,
    string? Commit,
    DateTimeOffset CreatedAt);
