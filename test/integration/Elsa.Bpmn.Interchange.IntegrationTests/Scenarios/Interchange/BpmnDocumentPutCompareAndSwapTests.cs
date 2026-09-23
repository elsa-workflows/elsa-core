using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
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
        var reader = services.GetRequiredService<BpmnXmlReader>();
        var sourceXml = (string)stored.CustomProperties[BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey];
        var firstEdit = reader.Read(sourceXml.Replace("Order Handled", "First writer"), new BpmnImportOptions()).Definitions;
        var secondEdit = reader.Read(sourceXml.Replace("Order Handled", "Second writer"), new BpmnImportOptions()).Definitions;

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

    [Fact(DisplayName = "A metadata-only save in the window is 412; the rename stays and the document PUT does not overwrite it")]
    public async Task ImportDocumentAsync_WhenMetadataIsSavedAfterTheFirstMatched_RefusesRatherThanRevertingTheRename()
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
        var reader = services.GetRequiredService<BpmnXmlReader>();
        var sourceXml = (string)stored.CustomProperties[BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey];
        var firstEdit = reader.Read(sourceXml.Replace("Order Handled", "First writer"), new BpmnImportOptions()).Definitions;

        var gate = new CompareAndSwapPauseGate();
        var pausingStore = new PausingCompareAndSwapStore(innerStore, gate);
        var firstWriter = ActivatorUtilities.CreateInstance<BpmnInterchangeDocumentService>(services, pausingStore);

        var first = firstWriter.ImportDocumentAsync(firstEdit, definitionId, processId: null, CancellationToken.None, expectedETag);
        await gate.Checked.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var latest = await FindLatestAsync(innerStore, definitionId);
        latest.Name = "Renamed-in-window";
        await innerStore.SaveAsync(latest);

        gate.Release.TrySetResult();

        var lost = await Assert.ThrowsAsync<BpmnDocumentPreconditionFailedException>(() => first);

        Assert.Contains("written since the ETag in If-Match was issued", lost.Message);

        var after = await FindLatestAsync(innerStore, definitionId);
        Assert.Equal("Renamed-in-window", after.Name);
        Assert.Equal(stored.StringData, after.StringData);
    }

    [Fact(DisplayName = "A metadata save during document PUT is preserved while the BPMN edit is applied")]
    public async Task ImportDocumentAsync_WhenMetadataIsSavedDuringCompareAndSwap_PreservesTheMetadataAndDocumentEdit()
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
        var reader = services.GetRequiredService<BpmnXmlReader>();
        var sourceXml = (string)stored.CustomProperties[BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey];
        var edit = reader.Read(sourceXml.Replace("Order Handled", "Document edit"), new BpmnImportOptions()).Definitions;

        var gate = new CompareAndSwapPauseGate();
        var pausingStore = new PausingCompareAndSwapStore(innerStore, gate);
        var documentWriter = ActivatorUtilities.CreateInstance<BpmnInterchangeDocumentService>(services, pausingStore);
        var documentPut = documentWriter.ImportDocumentAsync(edit, definitionId, processId: null, CancellationToken.None, expectedETag);
        await gate.Checked.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var metadataWriter = await FindLatestAsync(innerStore, definitionId);
        metadataWriter.Options.UsableAsActivity = true;
        metadataWriter.CustomProperties["test:concurrent-metadata"] = "preserve-me";
        await innerStore.SaveAsync(metadataWriter);

        gate.Release.TrySetResult();

        var result = await documentPut;
        Assert.True(result.ImportResult.Succeeded);

        var after = await FindLatestAsync(innerStore, definitionId);
        Assert.Equal("preserve-me", after.CustomProperties["test:concurrent-metadata"]);
        Assert.True(after.Options.UsableAsActivity);
        Assert.NotEqual(stored.StringData, after.StringData);
        Assert.NotEqual(expectedETag, BpmnDocumentETag.From(after));
    }

    [Fact(DisplayName = "A rejecting DraftSaving handler fails the document PUT before persist; the stored definition is unchanged")]
    public async Task ImportDocumentAsync_WhenDraftSavingHandlerRejects_DoesNotPersist()
    {
        var probe = new DraftNotificationProbe();
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .ConfigureServices(s =>
            {
                s.AddSingleton(probe);
                s.AddNotificationHandler<RejectingDraftSavingHandler>();
                s.AddNotificationHandler<CountingDraftSavedHandler>();
            })
            .Build();
        await services.PopulateRegistriesAsync();

        var store = services.GetRequiredService<IWorkflowDefinitionStore>();
        var setup = services.GetRequiredService<BpmnInterchangeDocumentService>();

        var imported = await setup.ImportAsync(ReadAsset("camunda-order-process.bpmn"), definitionId: null, name: "Order", processId: null, CancellationToken.None);
        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        var before = await FindLatestAsync(store, definitionId);
        var expectedETag = BpmnDocumentETag.From(before);
        var reader = services.GetRequiredService<BpmnXmlReader>();
        var sourceXml = (string)before.CustomProperties[BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey];
        var edit = reader.Read(sourceXml.Replace("Order Handled", "Must not persist"), new BpmnImportOptions()).Definitions;

        var savingBefore = probe.SavingCount;
        var savedBefore = probe.SavedCount;
        probe.Reject = true;

        var rejected = await Assert.ThrowsAsync<InvalidOperationException>(
            () => setup.ImportDocumentAsync(edit, definitionId, processId: null, CancellationToken.None, expectedETag));

        Assert.Equal("Draft save rejected.", rejected.Message);
        Assert.Equal(savingBefore + 1, probe.SavingCount);
        Assert.Equal(savedBefore, probe.SavedCount);

        var after = await FindLatestAsync(store, definitionId);
        Assert.Equal(expectedETag, BpmnDocumentETag.From(after));
        Assert.Equal(before.StringData, after.StringData);
        Assert.Equal(before.Name, after.Name);
    }

    [Fact(DisplayName = "A published→draft document PUT keeps the same id, version and created-at from DraftSaving through persist and DraftSaved")]
    public async Task ImportDocumentAsync_WhenLatestIsPublished_ReusesTheAnnouncedDraftIdentity()
    {
        var probe = new DraftNotificationProbe();
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .ConfigureServices(s =>
            {
                s.AddSingleton(probe);
                s.AddNotificationHandler<RejectingDraftSavingHandler>();
                s.AddNotificationHandler<CountingDraftSavedHandler>();
            })
            .Build();
        await services.PopulateRegistriesAsync();

        var store = services.GetRequiredService<IWorkflowDefinitionStore>();
        var setup = services.GetRequiredService<BpmnInterchangeDocumentService>();
        var publisher = services.GetRequiredService<IWorkflowDefinitionPublisher>();

        var imported = await setup.ImportAsync(ReadAsset("camunda-order-process.bpmn"), definitionId: null, name: "Order", processId: null, CancellationToken.None);
        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        await Support.DefinitionPublishing.PublishLatestAsync(publisher, definitionId);

        var published = await FindLatestAsync(store, definitionId);
        Assert.True(published.IsPublished);
        var expectedETag = BpmnDocumentETag.From(published);
        var reader = services.GetRequiredService<BpmnXmlReader>();
        var sourceXml = (string)published.CustomProperties[BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey];
        var edit = reader.Read(sourceXml.Replace("Order Handled", "After publish"), new BpmnImportOptions()).Definitions;

        probe.StampHandlerMarker = true;

        var result = await setup.ImportDocumentAsync(edit, definitionId, processId: null, CancellationToken.None, expectedETag);

        Assert.True(result.ImportResult.Succeeded);
        Assert.NotNull(probe.SavingId);
        Assert.Equal(probe.SavingId, probe.SavedId);
        Assert.Equal(probe.SavingVersion, probe.SavedVersion);
        Assert.Equal(probe.SavingCreatedAt, probe.SavedCreatedAt);
        Assert.NotEqual(published.Id, probe.SavingId);
        Assert.Equal(published.Version + 1, probe.SavingVersion);

        var persisted = result.ImportResult.WorkflowDefinition;
        Assert.Equal(probe.SavingId, persisted.Id);
        Assert.Equal(probe.SavingVersion, persisted.Version);
        Assert.Equal(probe.SavingCreatedAt, persisted.CreatedAt);

        var stored = await FindLatestAsync(store, definitionId);
        Assert.Equal(probe.SavingId, stored.Id);
        Assert.Equal(probe.SavingVersion, stored.Version);
        Assert.Equal(probe.SavingCreatedAt, stored.CreatedAt);
        Assert.False(stored.IsPublished);
        Assert.Equal("kept", stored.CustomProperties[DraftNotificationProbe.HandlerMarkerKey]);
        Assert.Equal("Order", stored.Name);
    }

    private static async Task<WorkflowDefinition> FindLatestAsync(IWorkflowDefinitionStore store, string definitionId)
    {
        var found = await store.FindAsync(WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter());
        return found ?? throw new InvalidOperationException($"Latest definition '{definitionId}' was not stored.");
    }

    private static string ReadAsset(string fileName) => Support.BpmnAssetReader.Read(fileName);

    private sealed class DraftNotificationProbe
    {
        public const string HandlerMarkerKey = "test:draft-saving-marker";

        public bool Reject { get; set; }
        public bool StampHandlerMarker { get; set; }
        public int SavingCount { get; set; }
        public int SavedCount { get; set; }
        public string? SavingId { get; set; }
        public int SavingVersion { get; set; }
        public DateTimeOffset SavingCreatedAt { get; set; }
        public string? SavedId { get; set; }
        public int SavedVersion { get; set; }
        public DateTimeOffset SavedCreatedAt { get; set; }
    }

    private sealed class RejectingDraftSavingHandler(DraftNotificationProbe probe) : INotificationHandler<WorkflowDefinitionDraftSaving>
    {
        public Task HandleAsync(WorkflowDefinitionDraftSaving notification, CancellationToken cancellationToken)
        {
            probe.SavingCount++;
            probe.SavingId = notification.WorkflowDefinition.Id;
            probe.SavingVersion = notification.WorkflowDefinition.Version;
            probe.SavingCreatedAt = notification.WorkflowDefinition.CreatedAt;

            if (probe.StampHandlerMarker)
                notification.WorkflowDefinition.CustomProperties[DraftNotificationProbe.HandlerMarkerKey] = "kept";

            if (probe.Reject)
                throw new InvalidOperationException("Draft save rejected.");

            return Task.CompletedTask;
        }
    }

    private sealed class CountingDraftSavedHandler(DraftNotificationProbe probe) : INotificationHandler<WorkflowDefinitionDraftSaved>
    {
        public Task HandleAsync(WorkflowDefinitionDraftSaved notification, CancellationToken cancellationToken)
        {
            probe.SavedCount++;
            probe.SavedId = notification.WorkflowDefinition.Id;
            probe.SavedVersion = notification.WorkflowDefinition.Version;
            probe.SavedCreatedAt = notification.WorkflowDefinition.CreatedAt;
            return Task.CompletedTask;
        }
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
