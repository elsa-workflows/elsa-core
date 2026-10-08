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

    [Fact(DisplayName = "Export single workflow without consumers returns single JSON file")]
    public async Task ExportEndpoint_WithoutConsumers_ReturnsSingleJson()
    {
        var response = await CreateClient().ExportAsync(GrandchildDefinitionId);
        var content = AssertSuccessAndGetContent(response, "Export");

        // Single-file export returns raw JSON (not a zip).
        using var doc = await JsonDocument.ParseAsync(content);
        var defId = doc.RootElement.GetProperty("definitionId").GetString();
        Assert.Equal(GrandchildDefinitionId, defId);
    }

    [Fact(DisplayName = "Export single workflow with consumers returns zip with transitive consumers")]
    public async Task ExportEndpoint_WithConsumers_IncludesAllInZip()
    {
        var response = await CreateClient().ExportAsync(GrandchildDefinitionId, includeConsumingWorkflows: true);
        var definitionIds = await ExtractDefinitionIdsFromZipAsync(AssertSuccessAndGetContent(response, "Export"));

        Assert.Contains(GrandchildDefinitionId, definitionIds);
        Assert.Contains(ChildDefinitionId, definitionIds);
        Assert.Contains(ParentDefinitionId, definitionIds);
    }

    [Fact(DisplayName = "Bulk export without consumers returns only the requested workflows")]
    public async Task BulkExportEndpoint_WithoutConsumers_ReturnsOnlyRequested()
    {
        var request = new BulkExportWorkflowDefinitionsRequest([GrandchildVersionId, ChildVersionId]);
        var response = await CreateClient().BulkExportAsync(request);
        var definitionIds = await ExtractDefinitionIdsFromZipAsync(AssertSuccessAndGetContent(response, "Bulk export"));

        Assert.Contains(GrandchildDefinitionId, definitionIds);
        Assert.Contains(ChildDefinitionId, definitionIds);
        Assert.DoesNotContain(ParentDefinitionId, definitionIds);
    }

    [Fact(DisplayName = "Bulk export with consumers includes transitive consumers in zip")]
    public async Task BulkExportEndpoint_WithConsumers_IncludesTransitiveConsumers()
    {
        // Export only the grandchild by version ID, with consumers included.
        var request = new BulkExportWorkflowDefinitionsRequest([GrandchildVersionId], IncludeConsumingWorkflows: true);
        var response = await CreateClient().BulkExportAsync(request);
        var definitionIds = await ExtractDefinitionIdsFromZipAsync(AssertSuccessAndGetContent(response, "Bulk export"));

        Assert.Contains(GrandchildDefinitionId, definitionIds);
        Assert.Contains(ChildDefinitionId, definitionIds);
        Assert.Contains(ParentDefinitionId, definitionIds);
    }

    private IWorkflowDefinitionsApi CreateClient() => WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();

    private static Stream AssertSuccessAndGetContent(IApiResponse<Stream> response, string operation)
    {
        Assert.True(response.IsSuccessStatusCode, $"{operation} failed with status {response.StatusCode}");
        Assert.NotNull(response.Content);
        return response.Content;
    }

    private static async Task<List<string>> ExtractDefinitionIdsFromZipAsync(Stream zipStream)
    {
        await using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);
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
