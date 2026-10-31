using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CShells.Lifecycle;
using Elsa.Platform.Integration.Models;
using Elsa.Platform.Integration.Options;
using Elsa.Platform.Integration.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Platform.Integration.UnitTests;

public class ElsaLoomRecipeArtifactApplierTests
{
    [Fact]
    public async Task ApplyAsync_WithInlineImportWorkflowDefinitionRecipe_ImportsWorkflowDefinition()
    {
        var importer = Substitute.For<IWorkflowDefinitionImporter>();
        var apiSerializer = Substitute.For<IApiSerializer>();
        var shellRegistry = Substitute.For<IShellRegistry>();
        SaveWorkflowDefinitionRequest? importedRequest = null;
        var model = new WorkflowDefinitionModel
        {
            DefinitionId = "workflow-1",
            Name = "Workflow 1"
        };
        apiSerializer.Deserialize<WorkflowDefinitionModel>(Arg.Any<string>()).Returns(model);
        importer.ImportAsync(Arg.Do<SaveWorkflowDefinitionRequest>(x => importedRequest = x), Arg.Any<CancellationToken>())
            .Returns(new ImportWorkflowResult(true, new WorkflowDefinition { DefinitionId = "workflow-1" }, []));

        var services = new ServiceCollection()
            .AddSingleton(importer)
            .AddSingleton(apiSerializer)
            .AddSingleton(Microsoft.Extensions.Options.Options.Create(new ElsaPlatformIntegrationOptions()))
            .BuildServiceProvider();
        var sut = new ElsaLoomRecipeArtifactApplier(services, shellRegistry);
        await using var artifactZip = CreateArtifact("""
            {
              "name": "deploy-workflow",
              "steps": [
                {
                  "id": "import-workflow",
                  "type": "elsa.import-workflow-definition",
                  "input": {
                    "workflowDefinition": {
                      "definitionId": "workflow-1",
                      "name": "Workflow 1"
                    }
                  }
                }
              ]
            }
            """);
        var artifact = CreateArtifactItem(artifactZip);

        var result = await sut.ApplyAsync(CreateCommand(artifact), artifact, artifactZip);

        Assert.True(result.Succeeded);
        Assert.NotNull(importedRequest);
        Assert.True(importedRequest.Publish);
        Assert.Same(model, importedRequest.Model);
        await shellRegistry.DidNotReceiveWithAnyArgs().ReloadAsync(default!, default);
    }

    [Fact]
    public async Task ApplyAsync_WithMissingCapability_ReturnsRejected()
    {
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<IWorkflowDefinitionImporter>())
            .AddSingleton(Substitute.For<IApiSerializer>())
            .AddSingleton(Microsoft.Extensions.Options.Options.Create(new ElsaPlatformIntegrationOptions()))
            .AddSingleton(Substitute.For<Elsa.Features.Contracts.IInstalledFeatureProvider>())
            .BuildServiceProvider();
        var sut = new ElsaLoomRecipeArtifactApplier(services, Substitute.For<IShellRegistry>());
        await using var artifactZip = CreateArtifact("""
            {
              "name": "verify-capability",
              "steps": [
                {
                  "id": "verify",
                  "type": "elsa.verify-capabilities",
                  "input": {
                    "capabilities": [ "missing.capability" ]
                  }
                }
              ]
            }
            """);
        var artifact = CreateArtifactItem(artifactZip);

        var result = await sut.ApplyAsync(CreateCommand(artifact), artifact, artifactZip);

        Assert.Equal(PlatformArtifactStatus.Rejected, result.Status);
        Assert.Contains(result.Diagnostics, x => x.Code == "ELSA_PLATFORM_CAPABILITY_MISSING");
    }

    private static MemoryStream CreateArtifact(string recipeJson)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("payload/recipes/recipe.json");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            writer.Write(recipeJson);
        }

        stream.Position = 0;
        return stream;
    }

    private static PlatformArtifactItem CreateArtifactItem(Stream artifactZip)
    {
        var digest = ComputeDigest(artifactZip);
        return new PlatformArtifactItem(
            Guid.NewGuid(),
            "recipe",
            "loom.recipe",
            "1.0",
            digest,
            "Recipe",
            "https://example.com/recipe.zip",
            PlatformArtifactStatus.Pending,
            null,
            null,
            null);
    }

    private static PlatformRuntimeCommand CreateCommand(PlatformArtifactItem artifact) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlatformRuntimeCommandAction.Deploy,
            PlatformRuntimeCommandStatus.Running,
            null,
            null,
            "test",
            null,
            null,
            null,
            null,
            1,
            null,
            null,
            null,
            null,
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            [artifact]);

    private static PlatformArtifactDigest ComputeDigest(Stream stream)
    {
        stream.Position = 0;
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        stream.Position = 0;
        return new PlatformArtifactDigest("sha256", Convert.ToHexString(hash).ToLowerInvariant());
    }
}
