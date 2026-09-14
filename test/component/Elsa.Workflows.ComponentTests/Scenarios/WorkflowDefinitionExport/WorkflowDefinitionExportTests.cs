using System.IO.Compression;
using System.Text.Json;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Refit;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowDefinitionExport;

/// <summary>
/// Tests for the Export endpoint variations using a simple Parent → Child → Grandchild hierarchy.
/// </summary>
public class WorkflowDefinitionExportTests(App app) : AppComponentTest(app)
{
    private const string GrandchildDefinitionId = "refgraph-grandchild";
    private const string GrandchildVersionId = "refgraph-grandchild-v1";
    private const string ChildDefinitionId = "refgraph-child";
    private const string ChildVersionId = "refgraph-child-v1";
    private const string ParentDefinitionId = "refgraph-parent";

    [Test]
    [DisplayName("Export single workflow without consumers returns single JSON file")]
    public async Task ExportEndpoint_WithoutConsumers_ReturnsSingleJson()
    {
        using var response = await CreateClient().ExportAsync(GrandchildDefinitionId);
        await using var content = await AssertSuccessAndGetContent(response, "Export");

        // Single-file export returns raw JSON (not a zip).
        using var doc = await JsonDocument.ParseAsync(content);
        var defId = doc.RootElement.GetProperty("definitionId").GetString();
        await Assert.That(defId).IsEqualTo(GrandchildDefinitionId);
    }

    [Test]
    [DisplayName("Export single workflow with consumers returns zip with transitive consumers")]
    public async Task ExportEndpoint_WithConsumers_IncludesAllInZip()
    {
        using var response = await CreateClient().ExportAsync(GrandchildDefinitionId, includeConsumingWorkflows: true);
        await using var content = await AssertSuccessAndGetContent(response, "Export");
        var definitionIds = await ExtractDefinitionIdsFromZipAsync(content);

        await Assert.That(definitionIds).Contains(GrandchildDefinitionId);
        await Assert.That(definitionIds).Contains(ChildDefinitionId);
        await Assert.That(definitionIds).Contains(ParentDefinitionId);
    }

    [Test]
    [DisplayName("Bulk export without consumers returns only the requested workflows")]
    public async Task BulkExportEndpoint_WithoutConsumers_ReturnsOnlyRequested()
    {
        var request = new BulkExportWorkflowDefinitionsRequest([GrandchildVersionId, ChildVersionId]);
        using var response = await CreateClient().BulkExportAsync(request);
        await using var content = await AssertSuccessAndGetContent(response, "Bulk export");
        var definitionIds = await ExtractDefinitionIdsFromZipAsync(content);

        await Assert.That(definitionIds).Contains(GrandchildDefinitionId);
        await Assert.That(definitionIds).Contains(ChildDefinitionId);
        await Assert.That(definitionIds).DoesNotContain(ParentDefinitionId);
    }

    [Test]
    [DisplayName("Bulk export with consumers includes transitive consumers in zip")]
    public async Task BulkExportEndpoint_WithConsumers_IncludesTransitiveConsumers()
    {
        // Export only the grandchild by version ID, with consumers included.
        var request = new BulkExportWorkflowDefinitionsRequest([GrandchildVersionId], IncludeConsumingWorkflows: true);
        using var response = await CreateClient().BulkExportAsync(request);
        await using var content = await AssertSuccessAndGetContent(response, "Bulk export");
        var definitionIds = await ExtractDefinitionIdsFromZipAsync(content);

        await Assert.That(definitionIds).Contains(GrandchildDefinitionId);
        await Assert.That(definitionIds).Contains(ChildDefinitionId);
        await Assert.That(definitionIds).Contains(ParentDefinitionId);
    }

    private IWorkflowDefinitionsApi CreateClient() => WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();

    private static async Task<Stream> AssertSuccessAndGetContent(IApiResponse<Stream> response, string operation)
    {
        await Assert.That(response.IsSuccessStatusCode).IsTrue().Because($"{operation} failed with status {response.StatusCode}");
        await Assert.That(response.Content).IsNotNull();
        return response.Content!;
    }

    private static async Task<List<string>> ExtractDefinitionIdsFromZipAsync(Stream zipStream)
    {
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        var definitionIds = new List<string>();

        foreach (var entry in zip.Entries)
        {
            await using var entryStream = await entry.OpenAsync();
            using var doc = await JsonDocument.ParseAsync(entryStream);
            if (doc.RootElement.TryGetProperty("definitionId", out var defIdProp))
                definitionIds.Add(defIdProp.GetString()!);
        }

        return definitionIds;
    }
}
