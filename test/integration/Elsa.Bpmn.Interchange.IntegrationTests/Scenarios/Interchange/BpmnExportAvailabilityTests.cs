using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact(DisplayName = "Exporting a definition that carries BPMN source without a recorded version is refused, distinctly from both other refusals")]
    public async Task Export_OfADefinitionCarryingSourceWithoutAVersionMarker_IsRefused()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded);

        var stored = await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);

        // ImportAsync itself always writes both BPMN markers together (see this finding's fix), so this combination
        // is not reachable through import; it is reproduced here directly to exercise the defence kept for the same
        // combination arising some other way — e.g. custom properties edited or migrated outside ImportAsync.
        stored.CustomProperties.Remove(BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey);
        await DefinitionStore.SaveAsync(stored);

        var refetched = await FindLatestAsync(stored.DefinitionId);

        var exception = Assert.Throws<BpmnExportUnavailableException>(() => DocumentService.Export(refetched));

        Assert.Contains("carries BPMN source, but not the definition version", exception.Message);
        Assert.DoesNotContain("does not currently carry BPMN source", exception.Message);
        Assert.DoesNotContain("has changed since it was imported", exception.Message);
    }

    [Fact(DisplayName = "A post-import save that fails leaves nothing durably carrying BPMN source, so the definition exports as never imported rather than as a partial import")]
    public async Task ImportAsync_WhenThePostImportSaveFails_LeavesNothingDurablyCarryingEitherMarker()
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .Build();
        await services.PopulateRegistriesAsync();

        // A stub importer stands in for the real IWorkflowDefinitionImporter/IWorkflowDefinitionStore pair so that
        // "durably persisted" can be told apart from "the local object ImportAsync goes on to mutate in memory
        // before the save that would have committed it fails" — a distinction a shared-reference in-memory test
        // double cannot make, but any real store (which only reflects a committed write) does.
        var draft = new WorkflowDefinition { Id = "draft-1", DefinitionId = "def-1", Version = 3, CustomProperties = new Dictionary<string, object>() };
        var importer = new StubWorkflowDefinitionImporter(draft);
        var store = new AlwaysFailingSaveWorkflowDefinitionStore();

        var documentService = new BpmnInterchangeDocumentService(
            services.GetRequiredService<BpmnXmlReader>(),
            services.GetRequiredService<BpmnXmlWriter>(),
            services.GetRequiredService<BpmnWorkBinder>(),
            importer,
            store);

        var xml = BpmnAssetReader.Read("camunda-order-process.bpmn");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => documentService.ImportAsync(xml, draft.DefinitionId, name: null, processId: null, CancellationToken.None));

        // What the importer's own (already-succeeded) save durably committed, captured independently of the `draft`
        // reference ImportAsync went on to mutate for the save that then failed. Neither BPMN marker made it in —
        // not just the version marker, which is all that would be missing under the old, two-save behaviour this
        // finding replaced.
        var persisted = new WorkflowDefinition { Id = draft.Id, DefinitionId = draft.DefinitionId, Version = draft.Version, CustomProperties = importer.CommittedCustomProperties };
        Assert.False(persisted.CustomProperties.ContainsKey(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey));
        Assert.False(persisted.CustomProperties.ContainsKey(BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey));

        var exception = Assert.Throws<BpmnExportUnavailableException>(() => documentService.Export(persisted));

        Assert.Contains("does not currently carry BPMN source", exception.Message);
        Assert.DoesNotContain("has changed since it was imported", exception.Message);
    }

    private async Task<WorkflowDefinition> FindLatestAsync(string definitionId)
    {
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await DefinitionStore.FindAsync(filter);
        Assert.NotNull(definition);
        return definition!;
    }

    /// <summary>
    /// Mirrors the one line of the real <see cref="Elsa.Workflows.Management.Services.WorkflowDefinitionImporter"/>
    /// this test cares about — <c>draft.CustomProperties = model.CustomProperties ?? new Dictionary&lt;...&gt;()</c> —
    /// so a regression that puts BPMN source back on the model passed into that call (the two-save behaviour this
    /// finding replaced) is caught here too, not just a regression in the save that follows.
    /// </summary>
    /// <remarks>
    /// <see cref="CommittedCustomProperties"/> is a defensive copy, captured at the moment this "import" succeeds,
    /// standing in for what a real store would have durably written — decoupled from <paramref name="definitionToReturn"/>,
    /// which the caller goes on to mutate in memory for the save this test makes fail.
    /// </remarks>
    private sealed class StubWorkflowDefinitionImporter(WorkflowDefinition definitionToReturn) : IWorkflowDefinitionImporter
    {
        public IDictionary<string, object> CommittedCustomProperties { get; private set; } = new Dictionary<string, object>();

        public Task<ImportWorkflowResult> ImportAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var modelCustomProperties = request.Model.CustomProperties ?? new Dictionary<string, object>();
            CommittedCustomProperties = new Dictionary<string, object>(modelCustomProperties);
            definitionToReturn.CustomProperties = new Dictionary<string, object>(modelCustomProperties);

            return Task.FromResult(new ImportWorkflowResult(true, definitionToReturn, new List<WorkflowValidationError>()));
        }
    }

    /// <summary>A store whose <see cref="SaveAsync"/> always fails; nothing else is exercised by the test that uses it.</summary>
    private sealed class AlwaysFailingSaveWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        public Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated save failure for testing atomic BPMN import.");

        public Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<long> CountDistinctAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> GetIsNameUnique(string name, string? definitionId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
