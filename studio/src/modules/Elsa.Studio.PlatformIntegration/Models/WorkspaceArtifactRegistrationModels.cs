namespace Elsa.Studio.PlatformIntegration;

internal sealed record WorkspaceArtifactRegistrationRequest(
    string ArtifactId,
    string LayoutVersion,
    PlatformArtifactDigest ContentDigest,
    string Format,
    string ReferenceProvider,
    string Reference,
    WorkspaceArtifactManifestSummary Manifest,
    IReadOnlyList<WorkspaceArtifactResourceSummary> Resources,
    IReadOnlyList<WorkspaceArtifactDiagnostic> Diagnostics,
    string? EnvelopeVersion = null,
    string? ArtifactTypeId = null,
    string? ArtifactSchemaVersion = null,
    PlatformArtifactDigest? ManifestDigest = null,
    PlatformArtifactPayloadReference? PayloadReference = null,
    PlatformArtifactProducer? Producer = null,
    PlatformArtifactDisplayMetadata? DisplayMetadata = null,
    IReadOnlyList<PlatformArtifactCompatibilityHint>? CompatibilityHints = null);

internal sealed record WorkspaceArtifactManifestSummary(string? Name, string? Version, string? Environment);

internal sealed record WorkspaceArtifactResourceSummary(
    string Type,
    string LogicalId,
    string? Scope,
    string? Version,
    PlatformArtifactDigest? DesiredStateHash);

internal sealed record WorkspaceArtifactDiagnostic(string Code, string Severity, string Message);

internal sealed record WorkspaceArtifactResponse(Guid Id, string ArtifactId, PlatformArtifactDigest ContentDigest, DateTimeOffset RegisteredAt);

internal sealed record ProblemDetailsResponse(string? Title, string? Detail);
