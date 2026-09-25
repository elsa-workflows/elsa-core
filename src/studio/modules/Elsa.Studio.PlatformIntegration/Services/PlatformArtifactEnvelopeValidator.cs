using System.Text.RegularExpressions;

namespace Elsa.Studio.PlatformIntegration.Services;

internal sealed partial class PlatformArtifactEnvelopeValidator
{
    private static readonly string[] UnsafeTerms =
    [
        "authorization",
        "bearer",
        "connectionstring",
        "connection string",
        "password",
        "private key",
        "secret",
        "token"
    ];

    public void Validate(PlatformArtifactEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.ArtifactId))
            throw new InvalidOperationException("Artifact identity is required.");
        if (!envelope.EnvelopeVersion.Equals(PlatformArtifactEnvelopeConstants.EnvelopeVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Artifact envelope version is not supported.");
        if (!envelope.ArtifactTypeId.Equals(PlatformArtifactEnvelopeConstants.ElsaLoomRecipeArtifactType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Artifact type is not supported.");
        if (!envelope.ArtifactSchemaVersion.Equals(PlatformArtifactEnvelopeConstants.DefaultArtifactSchemaVersion, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Artifact schema version is not supported for the artifact type.");

        ValidateDigest(envelope.ContentDigest.Algorithm, envelope.ContentDigest.Value, "Artifact content digest");
        ValidatePayloadReference(envelope.PayloadReference);
        ValidateProducer(envelope.Producer);
        ValidateDisplayMetadata(envelope.DisplayMetadata);
        ValidateCompatibilityHints(envelope.ArtifactTypeId, envelope.CompatibilityHints);
    }

    public string SafeMessage(string? value)
    {
        var safe = string.IsNullOrWhiteSpace(value) ? "Platform submission failed." : value.Trim();
        foreach (var term in UnsafeTerms)
            safe = safe.Replace(term, "[redacted]", StringComparison.OrdinalIgnoreCase);
        return safe.Length <= 512 ? safe : safe[..512];
    }

    private static void ValidateDigest(string algorithm, string value, string label)
    {
        if (!algorithm.Equals("sha256", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{label} is required.");
        if (!DigestValueRegex().IsMatch(value))
            throw new InvalidOperationException($"{label} has an invalid digest value.");
    }

    private void ValidatePayloadReference(PlatformArtifactPayloadReference reference)
    {
        if (string.IsNullOrWhiteSpace(reference.Provider) || string.IsNullOrWhiteSpace(reference.Uri))
            throw new InvalidOperationException("Artifact payload reference is required.");
        ValidateSafeValue(reference.Provider, "Artifact payload reference provider");
        ValidateSafeValue(reference.Uri, "Artifact payload reference URI");
        ValidateSafeValue(reference.MediaType, "Artifact payload media type");
        if (reference.ReferenceDigest is { } digest)
            ValidateDigest(digest.Algorithm, digest.Value, "Artifact reference digest");
    }

    private void ValidateProducer(PlatformArtifactProducer producer)
    {
        if (string.IsNullOrWhiteSpace(producer.ProducerType) || string.IsNullOrWhiteSpace(producer.ProducerName))
            throw new InvalidOperationException("Artifact producer metadata is required.");
        ValidateSafeValue(producer.ProducerType, "Artifact producer type");
        ValidateSafeValue(producer.ProducerName, "Artifact producer name");
        ValidateSafeValue(producer.ProducerVersion, "Artifact producer version");
        ValidateSafeValue(producer.SourceReference, "Artifact producer source reference");
    }

    private void ValidateDisplayMetadata(PlatformArtifactDisplayMetadata metadata)
    {
        ValidateSafeValue(metadata.Name, "Artifact display name");
        ValidateSafeValue(metadata.Version, "Artifact display version");
        ValidateSafeValue(metadata.Description, "Artifact display description");
        ValidateSafeValue(metadata.Source, "Artifact display source");
        ValidateSafeDictionary(metadata.Labels, "Artifact labels");
        ValidateSafeDictionary(metadata.Annotations, "Artifact annotations");
    }

    private void ValidateCompatibilityHints(string artifactTypeId, IReadOnlyList<PlatformArtifactCompatibilityHint> hints)
    {
        foreach (var hint in hints)
        {
            if (!hint.RequiredArtifactType.Equals(artifactTypeId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Artifact compatibility hint does not match the artifact type.");
            ValidateSafeValue(hint.RuntimeFamily, "Artifact runtime family");
            ValidateSafeValue(hint.RuntimeVersionRange, "Artifact runtime version range");
            foreach (var capability in hint.RequiredCapabilities)
                ValidateSafeValue(capability, "Artifact required capability");
            ValidateSafeDictionary(hint.EnvironmentConstraints, "Artifact environment constraints");
        }
    }

    private void ValidateSafeDictionary(IReadOnlyDictionary<string, string> values, string label)
    {
        if (values.Count > 50)
            throw new InvalidOperationException($"{label} contains too many entries.");
        foreach (var (key, value) in values)
        {
            ValidateSafeValue(key, label);
            ValidateSafeValue(value, label);
        }
    }

    private void ValidateSafeValue(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        if (UnsafeTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Artifact metadata contains unsafe secret-like content.");
        if (value.Length > 2048)
            throw new InvalidOperationException($"{label} is too long.");
    }

    [GeneratedRegex("^[A-Fa-f0-9]{64}$")]
    private static partial Regex DigestValueRegex();
}
