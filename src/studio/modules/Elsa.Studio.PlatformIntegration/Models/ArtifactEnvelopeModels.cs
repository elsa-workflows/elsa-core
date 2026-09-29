namespace Elsa.Studio.PlatformIntegration;

/// <summary>
/// Provides Platform artifact envelope constants.
/// </summary>
public static class PlatformArtifactEnvelopeConstants
{
    /// <summary>
    /// The supported artifact envelope version.
    /// </summary>
    public const string EnvelopeVersion = "platform.elsa.io/artifact-envelope/v1alpha1";

    /// <summary>
    /// The supported deployment artifact layout version.
    /// </summary>
    public const string LayoutVersion = "platform.elsa.io/deployment-artifact/v1alpha1";

    /// <summary>
    /// The default workflow artifact schema version.
    /// </summary>
    public const string DefaultArtifactSchemaVersion = "1.0";

    /// <summary>
    /// The Elsa Loom recipe artifact type.
    /// </summary>
    public const string ElsaLoomRecipeArtifactType = "elsa.loom.recipe";
}

/// <summary>
/// Represents an artifact envelope.
/// </summary>
public sealed record PlatformArtifactEnvelope(
    string ArtifactId,
    string EnvelopeVersion,
    string ArtifactTypeId,
    string ArtifactSchemaVersion,
    PlatformArtifactDigest ContentDigest,
    PlatformArtifactDigest? ManifestDigest,
    PlatformArtifactPayloadReference PayloadReference,
    PlatformArtifactProducer Producer,
    PlatformArtifactDisplayMetadata DisplayMetadata,
    IReadOnlyList<PlatformArtifactCompatibilityHint> CompatibilityHints,
    IReadOnlyList<PlatformArtifactEnvelopeDiagnostic> Diagnostics);

/// <summary>
/// Represents an artifact digest.
/// </summary>
public sealed record PlatformArtifactDigest(string Algorithm, string Value);

/// <summary>
/// Represents an artifact payload reference.
/// </summary>
public sealed record PlatformArtifactPayloadReference(
    string Provider,
    string Uri,
    string? MediaType = null,
    long? SizeBytes = null,
    PlatformArtifactDigest? ReferenceDigest = null,
    DateTimeOffset? ExpiresAt = null);

/// <summary>
/// Represents artifact producer metadata.
/// </summary>
public sealed record PlatformArtifactProducer(
    string ProducerType,
    string ProducerName,
    string? ProducerVersion = null,
    string? SourceReference = null);

/// <summary>
/// Represents artifact display metadata.
/// </summary>
public sealed record PlatformArtifactDisplayMetadata(
    string? Name,
    string? Version,
    string? Description,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<string, string> Annotations,
    string? Source = null);

/// <summary>
/// Represents an artifact compatibility hint.
/// </summary>
public sealed record PlatformArtifactCompatibilityHint(
    string RequiredArtifactType,
    string? RuntimeFamily,
    string? RuntimeVersionRange,
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyDictionary<string, string> EnvironmentConstraints);

/// <summary>
/// Represents an artifact envelope diagnostic.
/// </summary>
public sealed record PlatformArtifactEnvelopeDiagnostic(
    string Code,
    PlatformArtifactEnvelopeDiagnosticSeverity Severity,
    string Message,
    string? Target = null);

/// <summary>
/// Represents artifact envelope diagnostic severity.
/// </summary>
public enum PlatformArtifactEnvelopeDiagnosticSeverity
{
    /// <summary>
    /// Informational diagnostic.
    /// </summary>
    Info,

    /// <summary>
    /// Warning diagnostic.
    /// </summary>
    Warning,

    /// <summary>
    /// Error diagnostic.
    /// </summary>
    Error
}
