using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.PlatformIntegration.Contracts;

namespace Elsa.Studio.PlatformIntegration.Services;

internal sealed partial class PlatformWorkflowSnapshotPackager(
    PlatformArtifactEnvelopeValidator? validator = null) : IPlatformWorkflowSnapshotPackager
{
    private static readonly JsonSerializerOptions WorkflowJsonOptions = CreateWorkflowJsonOptions();
    private readonly PlatformArtifactEnvelopeValidator _validator = validator ?? new();

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "WorkflowDefinition is the Studio API client model already serialized dynamically by Studio workflow import/export services.")]
    public PlatformWorkflowSubmitPackage Package(WorkflowDefinition workflowDefinition, PlatformSubmitOptions options, DateTimeOffset? packagedAt = null)
    {
        ValidateWorkflowDefinition(workflowDefinition);
        ValidateOptions(options);

        var definitionId = workflowDefinition.DefinitionId!.Trim();
        var displayName = workflowDefinition.Name!.Trim();
        var workflowDefinitionJson = JsonSerializer.Serialize(workflowDefinition, WorkflowJsonOptions);
        var recipeJson = BuildRecipeJson(workflowDefinition, workflowDefinitionJson);
        var contentDigest = ComputeDigest(recipeJson);
        var payloadReference = new PlatformArtifactPayloadReference(
            options.PayloadProvider.Trim(),
            BuildPayloadUri(options.PayloadUriScheme, definitionId, contentDigest.Value),
            "application/vnd.elsa.loom.recipe+json",
            Encoding.UTF8.GetByteCount(recipeJson),
            contentDigest);

        var envelope = new PlatformArtifactEnvelope(
            BuildArtifactId(definitionId, contentDigest.Value),
            PlatformArtifactEnvelopeConstants.EnvelopeVersion,
            PlatformArtifactEnvelopeConstants.ElsaLoomRecipeArtifactType,
            options.ArtifactSchemaVersion.Trim(),
            contentDigest,
            null,
            payloadReference,
            new PlatformArtifactProducer(
                "studio",
                options.ProducerName.Trim(),
                TrimToNull(options.ProducerVersion),
                BuildSourceReference(definitionId, workflowDefinition.Id)),
            new PlatformArtifactDisplayMetadata(
                displayName,
                workflowDefinition.Version.ToString(CultureInfo.InvariantCulture),
                TrimToNull(workflowDefinition.Description),
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                BuildWorkflowSource(definitionId)),
            [
                new PlatformArtifactCompatibilityHint(
                    PlatformArtifactEnvelopeConstants.ElsaLoomRecipeArtifactType,
                    "elsa-workflows",
                    TrimToNull(options.RuntimeVersionRange),
                    options.RequiredCapabilities
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    new Dictionary<string, string>())
            ],
            []);

        _validator.Validate(envelope);
        return new PlatformWorkflowSubmitPackage(envelope, recipeJson, (packagedAt ?? DateTimeOffset.UtcNow).ToUniversalTime());
    }

    private static void ValidateWorkflowDefinition(WorkflowDefinition workflowDefinition)
    {
        if (string.IsNullOrWhiteSpace(workflowDefinition.DefinitionId))
            throw new InvalidOperationException("Workflow definition identity is required before submitting to Platform.");
        if (string.IsNullOrWhiteSpace(workflowDefinition.Name))
            throw new InvalidOperationException("Workflow display name is required before submitting to Platform.");
        if (workflowDefinition.Root is null)
            throw new InvalidOperationException("Workflow definition snapshot is required before submitting to Platform.");
    }

    private static void ValidateOptions(PlatformSubmitOptions options)
    {
        if (options.PlatformEndpoint is null)
            throw new InvalidOperationException("Platform endpoint is required before submitting to Platform.");
        if (options.WorkspaceId is null || options.WorkspaceId == Guid.Empty)
            throw new InvalidOperationException("Platform workspace is required before submitting to Platform.");
        if (string.IsNullOrWhiteSpace(options.ProducerName))
            throw new InvalidOperationException("Studio producer name is required before submitting to Platform.");
        if (string.IsNullOrWhiteSpace(options.PayloadProvider))
            throw new InvalidOperationException("Artifact payload provider is required before submitting to Platform.");
        if (string.IsNullOrWhiteSpace(options.PayloadUriScheme))
            throw new InvalidOperationException("Artifact payload URI scheme is required before submitting to Platform.");
        if (string.IsNullOrWhiteSpace(options.ArtifactSchemaVersion))
            throw new InvalidOperationException("Workflow artifact schema version is required before submitting to Platform.");
    }

    private static PlatformArtifactDigest ComputeDigest(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return new PlatformArtifactDigest("sha256", Convert.ToHexString(hash).ToLowerInvariant());
    }

    private static string BuildRecipeJson(WorkflowDefinition workflowDefinition, string workflowDefinitionJson)
    {
        var workflowDefinitionNode = JsonNode.Parse(workflowDefinitionJson)
            ?? throw new InvalidOperationException("Workflow definition snapshot is not valid JSON.");
        var definitionId = workflowDefinition.DefinitionId!.Trim();
        var recipe = new JsonObject
        {
            ["schemaVersion"] = "1.0",
            ["id"] = definitionId,
            ["name"] = workflowDefinition.Name!.Trim(),
            ["version"] = workflowDefinition.Version.ToString(CultureInfo.InvariantCulture),
            ["steps"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = $"upsert-{SafeIdentitySegment(definitionId)}",
                    ["type"] = "workflowDefinition.upsert",
                    ["publish"] = true,
                    ["payload"] = workflowDefinitionNode
                }
            }
        };

        return recipe.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string BuildArtifactId(string workflowDefinitionId, string digest) =>
        $"elsa.loom.recipe:{SafeIdentitySegment(workflowDefinitionId)}:{digest}";

    private static string BuildPayloadUri(string scheme, string workflowDefinitionId, string digest) =>
        $"{scheme.Trim()}://loom-recipes/{Uri.EscapeDataString(workflowDefinitionId.Trim())}/snapshots/{digest}";

    private static string BuildWorkflowSource(string workflowDefinitionId) =>
        $"studio://workflows/{Uri.EscapeDataString(workflowDefinitionId.Trim())}";

    private static string BuildSourceReference(string workflowDefinitionId, string? workflowDefinitionVersionId)
    {
        var workflowId = workflowDefinitionId.Trim();
        var versionId = TrimToNull(workflowDefinitionVersionId);
        return versionId is null ? $"workflow:{workflowId}" : $"workflow:{workflowId}:version:{versionId}";
    }

    private static string SafeIdentitySegment(string value)
    {
        var safe = UnsafeIdentityCharacters().Replace(value.Trim(), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(safe))
            throw new InvalidOperationException("Workflow definition identity does not contain a safe artifact identity segment.");
        if (safe.Length <= 96)
            return safe;

        var identityDigest = ComputeDigest(value.Trim()).Value[..16];
        return $"{safe[..79]}-{identityDigest}";
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static JsonSerializerOptions CreateWorkflowJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }

    [GeneratedRegex("[^A-Za-z0-9_.-]+")]
    private static partial Regex UnsafeIdentityCharacters();
}
