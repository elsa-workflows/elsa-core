using Bpmn.Model;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;
using Elsa.Bpmn.Interchange.Exceptions;
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
/// The document PUT's precondition and save are one compare-and-swap (#8064). A store double pauses the first
/// writer after its match and before its save so a second writer can finish in that window; the first then gets
/// the same refusal a stale If-Match gets, and the second writer's change is what stays stored.
/// </summary>
public class BpmnDocumentPutCompareAndSwapTests(ITestOutputHelper testOutputHelper)
{
    [Fact(DisplayName = "A second document edit that wins the race leaves its change stored; the first writer is refused")]
    public async Task ImportDocumentAsync_WhenASecondWriterSavesAfterTheFirstMatched_RefusesTheFirstAndKeepsTheSecond()
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .Build();
        await services.PopulateRegistriesAsync();

        var innerStore = services.GetRequiredService<IWorkflowDefinitionStore>();
        var setup = services.GetRequiredService<BpmnInterchangeDocumentService>();

        var imported = await setup.ImportAsync(ReadAsset("camunda-order-process.bpmn"), definitionId: null, name: null, processId: null, CancellationToken.None);
        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        var stored = await FindLatestAsync(innerStore, definitionId);
        var expectedETag = BpmnDocumentETag.From(stored);
        var document = setup.ReadDocument(stored);

        var firstEdit = WithFirstElementRenamed(document, "First writer");
        var secondEdit = WithFirstElementRenamed(document, "Second writer");

        var gate = new CompareAndSwapPauseGate();
        var pausingStore = new PausingCompareAndSwapStore(innerStore, gate);
        var firstWriter = ActivatorUtilities.CreateInstance<BpmnInterchangeDocumentService>(services, pausingStore);

        var first = firstWriter.ImportDocumentAsync(firstEdit, definitionId, processId: null, CancellationToken.None, expectedETag);
        await gate.Checked.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var second = await setup.ImportDocumentAsync(secondEdit, definitionId, processId: null, CancellationToken.None, expectedETag);
        Assert.True(second.ImportResult.Succeeded);

        gate.Release.TrySetResult();

        var lost = await Assert.ThrowsAsync<BpmnDocumentPreconditionFailedException>(() => first);

        Assert.Contains("written since the ETag in If-Match was issued", lost.Message);

        var after = await FindLatestAsync(innerStore, definitionId);
        Assert.Equal(BpmnDocumentETag.From(second.ImportResult.WorkflowDefinition), BpmnDocumentETag.From(after));
        Assert.NotEqual(expectedETag, BpmnDocumentETag.From(after));
    }

    [Fact(DisplayName = "A metadata-only save in the window is carried onto the document PUT, not reverted")]
    public async Task ImportDocumentAsync_WhenMetadataIsSavedAfterTheFirstMatched_KeepsThatMetadata()
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .Build();
        await services.PopulateRegistriesAsync();

        var innerStore = services.GetRequiredService<IWorkflowDefinitionStore>();
        var setup = services.GetRequiredService<BpmnInterchangeDocumentService>();

        var imported = await setup.ImportAsync(ReadAsset("camunda-order-process.bpmn"), definitionId: null, name: "Order", processId: null, CancellationToken.None);
        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        var stored = await FindLatestAsync(innerStore, definitionId);
        var expectedETag = BpmnDocumentETag.From(stored);
        var document = setup.ReadDocument(stored);
        var firstEdit = WithFirstElementRenamed(document, "First writer");

        var gate = new CompareAndSwapPauseGate();
        var pausingStore = new PausingCompareAndSwapStore(innerStore, gate);
        var firstWriter = ActivatorUtilities.CreateInstance<BpmnInterchangeDocumentService>(services, pausingStore);

        var first = firstWriter.ImportDocumentAsync(firstEdit, definitionId, processId: null, CancellationToken.None, expectedETag);
        await gate.Checked.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var latest = await FindLatestAsync(innerStore, definitionId);
        latest.Name = "Renamed-in-window";
        await innerStore.SaveAsync(latest);

        gate.Release.TrySetResult();

        var result = await first;

        Assert.True(result.ImportResult.Succeeded);
        var after = await FindLatestAsync(innerStore, definitionId);
        Assert.Equal("Renamed-in-window", after.Name);
        Assert.NotEqual(expectedETag, BpmnDocumentETag.From(after));
    }

    private static async Task<WorkflowDefinition> FindLatestAsync(IWorkflowDefinitionStore store, string definitionId)
    {
        var found = await store.FindAsync(WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter());
        return found ?? throw new InvalidOperationException($"Latest definition '{definitionId}' was not stored.");
    }

    private static string ReadAsset(string fileName) => Support.BpmnAssetReader.Read(fileName);

    /// <summary>
    /// Renames the first element so the stored source — and therefore the ETag — moves, without needing a second
    /// writer to touch metadata.
    /// </summary>
    private static BpmnDefinitions WithFirstElementRenamed(BpmnDefinitions document, string name)
    {
        var process = Assert.Single(document.Processes);
        var elements = process.Elements.ToList();
        elements[0] = process.Elements[0] with { Name = name };
        return document with { Processes = [process with { Elements = elements }] };
    }

    /// <summary>
    /// Signals the test can run the second writer (<see cref="Checked"/>) and then lets the first writer resume
    /// (<see cref="Release"/>). Not a lock: the first writer is parked between match and save so the race is
    /// deterministic.
    /// </summary>
    private sealed class CompareAndSwapPauseGate
    {
        public TaskCompletionSource Checked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// A store double that implements the compare-and-swap contract with an explicit pause after the match:
    /// the first writer stops there, the second writer finishes on the inner store, then this re-reads and
    /// re-checks so a lost race is Conflict instead of an overwrite.
    /// </summary>
    private sealed class PausingCompareAndSwapStore(IWorkflowDefinitionStore inner, CompareAndSwapPauseGate gate) : IWorkflowDefinitionStore
    {
        public Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
            inner.FindAsync(filter, cancellationToken);

        public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            inner.FindAsync(filter, order, cancellationToken);

        public Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
            inner.FindManyAsync(filter, pageArgs, cancellationToken);

        public Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
            inner.FindManyAsync(filter, order, pageArgs, cancellationToken);

        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
            inner.FindManyAsync(filter, cancellationToken);

        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            inner.FindManyAsync(filter, order, cancellationToken);

        public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
            inner.FindSummariesAsync(filter, pageArgs, cancellationToken);

        public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
            inner.FindSummariesAsync(filter, order, pageArgs, cancellationToken);

        public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
            inner.FindSummariesAsync(filter, cancellationToken);

        public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            inner.FindSummariesAsync(filter, order, cancellationToken);

        public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken) =>
            inner.FindLastVersionAsync(filter, cancellationToken);

        public Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) =>
            inner.SaveAsync(definition, cancellationToken);

        public async Task<WorkflowDefinitionUpdateResult> TryUpdateLatestAsync(
            WorkflowDefinitionFilter filter,
            Func<WorkflowDefinition, bool> matchesExpected,
            Func<WorkflowDefinition, WorkflowDefinition> update,
            CancellationToken cancellationToken = default)
        {
            var current = await inner.FindAsync(filter, cancellationToken);

            if (current is null)
                return WorkflowDefinitionUpdateResult.NotFound();

            if (!matchesExpected(current))
                return WorkflowDefinitionUpdateResult.Conflict();

            gate.Checked.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);

            current = await inner.FindAsync(filter, cancellationToken);

            if (current is null)
                return WorkflowDefinitionUpdateResult.NotFound();

            if (!matchesExpected(current))
                return WorkflowDefinitionUpdateResult.Conflict();

            var next = update(current);
            await inner.SaveAsync(next, cancellationToken);
            return WorkflowDefinitionUpdateResult.Updated(next);
        }

        public Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default) =>
            inner.SaveManyAsync(definitions, cancellationToken);

        public Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(filter, cancellationToken);

        public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
            inner.AnyAsync(filter, cancellationToken);

        public Task<long> CountDistinctAsync(CancellationToken cancellationToken = default) =>
            inner.CountDistinctAsync(cancellationToken);

        public Task<bool> GetIsNameUnique(string name, string? definitionId = null, CancellationToken cancellationToken = default) =>
            inner.GetIsNameUnique(name, definitionId, cancellationToken);
    }
}
