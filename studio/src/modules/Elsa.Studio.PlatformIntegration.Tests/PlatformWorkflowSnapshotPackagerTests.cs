using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.PlatformIntegration.Services;
using Xunit;

namespace Elsa.Studio.PlatformIntegration.Tests;

public sealed class PlatformWorkflowSnapshotPackagerTests
{
    private readonly PlatformWorkflowSnapshotPackager _packager = new();
    private readonly PlatformSubmitOptions _options = new()
    {
        PlatformEndpoint = new Uri("https://platform.example.test"),
        WorkspaceId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        ProducerVersion = "4.0.0",
        RuntimeVersionRange = ">=4.0.0"
    };

    [Fact]
    public void Packages_workflow_definition_as_platform_artifact_envelope()
    {
        var packagedAt = DateTimeOffset.Parse("2026-05-29T08:00:00Z");

        var package = _packager.Package(WorkflowDefinition(), _options, packagedAt);

        Assert.Equal(packagedAt, package.PackagedAt);
        Assert.StartsWith("elsa.loom.recipe:payment-retry:", package.Envelope.ArtifactId);
        Assert.Equal(PlatformArtifactEnvelopeConstants.ElsaLoomRecipeArtifactType, package.Envelope.ArtifactTypeId);
        Assert.Equal(PlatformArtifactEnvelopeConstants.EnvelopeVersion, package.Envelope.EnvelopeVersion);
        Assert.Equal(PlatformArtifactEnvelopeConstants.DefaultArtifactSchemaVersion, package.Envelope.ArtifactSchemaVersion);
        Assert.Equal("sha256", package.Envelope.ContentDigest.Algorithm);
        Assert.Equal(64, package.Envelope.ContentDigest.Value.Length);
        Assert.Equal("producer-managed", package.Envelope.PayloadReference.Provider);
        Assert.StartsWith("studio://loom-recipes/payment-retry/snapshots/", package.Envelope.PayloadReference.Uri);
        Assert.Equal(package.Envelope.ContentDigest, package.Envelope.PayloadReference.ReferenceDigest);
        Assert.Equal("studio", package.Envelope.Producer.ProducerType);
        Assert.Equal("Elsa Studio", package.Envelope.Producer.ProducerName);
        Assert.Equal("4.0.0", package.Envelope.Producer.ProducerVersion);
        Assert.Equal("workflow:payment-retry:version:workflow-version-42", package.Envelope.Producer.SourceReference);
        Assert.Equal("Payment Retry", package.Envelope.DisplayMetadata.Name);
        Assert.Equal("42", package.Envelope.DisplayMetadata.Version);
        Assert.Single(package.Envelope.CompatibilityHints);
        Assert.Contains("loom.recipe.apply", package.Envelope.CompatibilityHints.Single().RequiredCapabilities);

        using var document = JsonDocument.Parse(package.WorkflowDefinitionJson);
        Assert.Equal("1.0", document.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal("payment-retry", document.RootElement.GetProperty("id").GetString());
        var step = document.RootElement.GetProperty("steps").EnumerateArray().Single();
        Assert.Equal("workflowDefinition.upsert", step.GetProperty("type").GetString());
        Assert.True(step.GetProperty("publish").GetBoolean());
        Assert.Equal("payment-retry", step.GetProperty("payload").GetProperty("definitionId").GetString());
    }

    [Fact]
    public void Uses_stable_artifact_identity_for_duplicate_snapshot()
    {
        var first = _packager.Package(WorkflowDefinition(), _options);
        var second = _packager.Package(WorkflowDefinition(), _options);

        Assert.Equal(first.Envelope.ArtifactId, second.Envelope.ArtifactId);
        Assert.Equal(first.Envelope.ContentDigest, second.Envelope.ContentDigest);
        Assert.Equal(first.Envelope.PayloadReference.Uri, second.Envelope.PayloadReference.Uri);
    }

    [Fact]
    public void Rejects_missing_platform_configuration()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _packager.Package(WorkflowDefinition(), _options with { PlatformEndpoint = null }));

        Assert.Equal("Platform endpoint is required before submitting to Platform.", exception.Message);
    }

    [Fact]
    public void Rejects_unsafe_metadata()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _packager.Package(WorkflowDefinition(description: "contains password"), _options));

        Assert.Equal("Artifact metadata contains unsafe secret-like content.", exception.Message);
    }

    private static WorkflowDefinition WorkflowDefinition(string description = "Retries payment collection failures.") =>
        new()
        {
            Id = "workflow-version-42",
            DefinitionId = "payment-retry",
            Name = "Payment Retry",
            Version = 42,
            Description = description,
            Root = new JsonObject
            {
                ["type"] = "Elsa.Flowchart",
                ["version"] = 1
            }
        };
}
