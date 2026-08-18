using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Interchange;

/// <summary>
/// <see cref="BpmnInterchangeDocumentService.Export(WorkflowDefinition)"/> refuses rather than mislead when the
/// stored BPMN source is missing or stale, instead of silently exporting a document the caller does not actually
/// have. See that type's remarks for why each situation gets its own message.
/// </summary>
public class BpmnExportAvailabilityTests(ITestOutputHelper testOutputHelper) : BpmnInterchangeTestBase(testOutputHelper)
{
    [Fact(DisplayName = "Exporting a freshly imported definition through the WorkflowDefinition overload succeeds")]
    public async Task Export_OfAFreshlyImportedDefinition_Succeeds()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded);

        var stored = await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);

        var bytes = DocumentService.Export(stored);

        Assert.NotEmpty(bytes);
    }

    [Fact(DisplayName = "Import records the definition's own version alongside the BPMN source")]
    public async Task Import_RecordsTheSourceVersionAlongsideTheSourceXml()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded);

        var stored = await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);

        Assert.True(stored.CustomProperties.TryGetValue<int>(BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey, out var sourceVersion));
        Assert.Equal(stored.Version, sourceVersion);
    }

    [Fact(DisplayName = "Exporting a definition whose custom properties no longer carry the BPMN source is refused, naming that as the reason")]
    public async Task Export_OfADefinitionWithoutStoredSource_IsRefused()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded);

        var stored = await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);

        // Mirrors what Elsa.Workflows.Api's workflow-definition save endpoint does to an edit that does not round-trip
        // custom properties: it replaces CustomProperties wholesale, dropping the BPMN source as a side effect of
        // saving something else entirely.
        stored.CustomProperties = new Dictionary<string, object>();
        await DefinitionStore.SaveAsync(stored);

        var refetched = await FindLatestAsync(stored.DefinitionId);

        var exception = Assert.Throws<BpmnExportUnavailableException>(() => DocumentService.Export(refetched));

        Assert.Contains("does not currently carry BPMN source", exception.Message);
        Assert.DoesNotContain("has changed since it was imported", exception.Message);
    }

    [Fact(DisplayName = "Exporting a definition that changed since it was imported is refused, naming that as the reason rather than returning the pre-edit document")]
    public async Task Export_OfADefinitionThatHasChangedSinceImport_IsRefused()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded);

        var stored = await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);

        // Mirrors the case the source key survives a later save (unlike the test above), but the definition itself
        // has moved on to a new version since the source was recorded — e.g. a publish followed by another edit.
        stored.Version += 1;
        await DefinitionStore.SaveAsync(stored);

        var refetched = await FindLatestAsync(stored.DefinitionId);

        var exception = Assert.Throws<BpmnExportUnavailableException>(() => DocumentService.Export(refetched));

        Assert.Contains("has changed since it was imported", exception.Message);
        Assert.DoesNotContain("does not currently carry BPMN source", exception.Message);
    }

    [Fact(DisplayName = "Exporting a definition whose import stored the source but never recorded its version marker is refused, distinctly from both other refusals")]
    public async Task Export_OfAPartiallyImportedDefinition_IsRefused()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded);

        var stored = await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);

        // Mirrors what a cancelled or failed second save leaves behind: ImportAsync's first save records SourceXml,
        // and only a second save records SourceVersion (see BpmnInterchangeDocumentService's remarks on why the
        // version cannot be known until the first save assigns it). If that second save never happens, the source is
        // there but the version marker it would have carried is not.
        stored.CustomProperties.Remove(BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey);
        await DefinitionStore.SaveAsync(stored);

        var refetched = await FindLatestAsync(stored.DefinitionId);

        var exception = Assert.Throws<BpmnExportUnavailableException>(() => DocumentService.Export(refetched));

        Assert.Contains("did not finish", exception.Message);
        Assert.DoesNotContain("does not currently carry BPMN source", exception.Message);
        Assert.DoesNotContain("has changed since it was imported", exception.Message);
    }

    private async Task<WorkflowDefinition> FindLatestAsync(string definitionId)
    {
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await DefinitionStore.FindAsync(filter);
        Assert.NotNull(definition);
        return definition!;
    }
}
