using System.Reflection;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Refit;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="DiagramDesignerWrapper.RefreshElementStatsAsync"/>: the element-keyed BPMN overlay's own
/// refresh channel, alongside the existing activity-keyed <see cref="DiagramDesignerWrapper.UpdateActivityStatsAsync"/>
/// one. Exercises the wrapper directly (constructed with <c>new</c>, its injected properties and private fields set
/// through reflection) rather than through bUnit rendering, since none of what is under test here depends on the
/// render pipeline.
/// </summary>
public class DiagramDesignerWrapperElementStatsTests
{
    [Fact(DisplayName = "Does nothing when there is no workflow instance to read from")]
    public async Task RefreshElementStatsAsync_NoWorkflowInstanceId_DoesNotCallTheJournal()
    {
        var journal = new RecordingWorkflowInstanceService();
        var wrapper = CreateWrapper(journal, new RecordingSinkDesigner(), workflowInstanceId: null);

        await wrapper.RefreshElementStatsAsync();

        Assert.Empty(journal.Calls);
    }

    [Fact(DisplayName = "Does nothing when the current designer does not accept an element-keyed overlay")]
    public async Task RefreshElementStatsAsync_DesignerIsNotASink_DoesNotCallTheJournal()
    {
        var journal = new RecordingWorkflowInstanceService();
        var wrapper = CreateWrapper(journal, new NonSinkDesigner(), workflowInstanceId: "instance-1");

        await wrapper.RefreshElementStatsAsync();

        Assert.Empty(journal.Calls);
    }

    [Fact(DisplayName = "Does nothing when the workflow carries no Elsa.BpmnProcess scope at all")]
    public async Task RefreshElementStatsAsync_NoBpmnProcessInTheGraph_DoesNotCallTheJournal()
    {
        var journal = new RecordingWorkflowInstanceService();
        var wrapper = CreateWrapper(journal, new RecordingSinkDesigner(), workflowInstanceId: "instance-1", includeBpmnProcess: false);

        await wrapper.RefreshElementStatsAsync();

        Assert.Empty(journal.Calls);
    }

    [Fact(DisplayName = "Filters the journal by every Elsa.BpmnProcess activity id in the whole graph, including a nested scope")]
    public async Task RefreshElementStatsAsync_FiltersByEveryBpmnProcessActivityId()
    {
        var journal = new RecordingWorkflowInstanceService(
            new PagedListResponse<WorkflowExecutionLogRecord> { Items = [] });
        var wrapper = CreateWrapper(journal, new RecordingSinkDesigner(), workflowInstanceId: "instance-1");

        await wrapper.RefreshElementStatsAsync();

        var call = Assert.Single(journal.Calls);
        Assert.Equal(new[] { "bpmn-outer", "bpmn-inner" }, call.Filter?.ActivityIds);
    }

    [Fact(DisplayName = "Folds the fetched journal and pushes the result to the current designer's sink")]
    public async Task RefreshElementStatsAsync_FoldsTheJournal_AndPushesItToTheSink()
    {
        var entry = DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "join");
        var journal = new RecordingWorkflowInstanceService(
            new PagedListResponse<WorkflowExecutionLogRecord> { Items = [entry] });
        var sink = new RecordingSinkDesigner();
        var wrapper = CreateWrapper(journal, sink, workflowInstanceId: "instance-1");

        await wrapper.RefreshElementStatsAsync();

        Assert.Equal(1, sink.UpdateCallCount);
        Assert.True(sink.ReceivedStats!["join"].Blocked);
    }

    [Fact(DisplayName = "Pages through the journal until a short page ends it, within a single refresh's page cap")]
    public async Task RefreshElementStatsAsync_PagesThroughTheJournal()
    {
        var fullPage = Enumerable.Range(0, 200).Select(i => DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, flowId: $"flow-{i}")).ToArray();
        var shortPage = new[] { DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "join") };
        var journal = new RecordingWorkflowInstanceService(
            new PagedListResponse<WorkflowExecutionLogRecord> { Items = fullPage },
            new PagedListResponse<WorkflowExecutionLogRecord> { Items = shortPage });
        var sink = new RecordingSinkDesigner();
        var wrapper = CreateWrapper(journal, sink, workflowInstanceId: "instance-1");

        await wrapper.RefreshElementStatsAsync();

        Assert.Equal(2, journal.Calls.Count);
        Assert.Equal(0, journal.Calls[0].Skip);
        Assert.Equal(200, journal.Calls[1].Skip);
        Assert.True(sink.ReceivedStats!["join"].Blocked);
        Assert.Equal(201, sink.ReceivedStats!.Count);
    }

    [Fact(DisplayName = "A backlog bigger than one refresh's page cap is folded a cap's worth at a time, continued on the next refresh")]
    public async Task RefreshElementStatsAsync_BacklogLargerThanPageCap_IsFoldedAcrossRefreshes()
    {
        // Three full pages of 200 records each, plus a short page that ends the journal -- 650 total -- against a
        // page cap of 2, so no single refresh fetches more than 2 pages (400 records) at once.
        var fullPages = Enumerable.Range(0, 3)
            .Select(p => new PagedListResponse<WorkflowExecutionLogRecord>
            {
                Items = Enumerable.Range(0, 200)
                    .Select(i => DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, flowId: $"flow-{p}-{i}"))
                    .ToArray()
            });
        var shortPage = new PagedListResponse<WorkflowExecutionLogRecord>
        {
            Items = Enumerable.Range(0, 50).Select(i => DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, flowId: $"flow-tail-{i}")).ToArray()
        };
        var journal = new RecordingWorkflowInstanceService([.. fullPages, shortPage]);
        var sink = new RecordingSinkDesigner();
        var wrapper = CreateWrapper(journal, sink, workflowInstanceId: "instance-1");

        wrapper.ElementStatsMaxPagesPerRefresh = 2;

        await wrapper.RefreshElementStatsAsync();

        Assert.Equal(2, journal.Calls.Count);
        Assert.Equal(0, journal.Calls[0].Skip);
        Assert.Equal(200, journal.Calls[1].Skip);
        Assert.Equal(400, sink.ReceivedStats!.Count);

        await wrapper.RefreshElementStatsAsync();

        Assert.Equal(4, journal.Calls.Count);
        Assert.Equal(400, journal.Calls[2].Skip);
        Assert.Equal(600, journal.Calls[3].Skip);
        Assert.Equal(650, sink.ReceivedStats!.Count);
    }

    [Fact(DisplayName = "A second refresh after new records arrive requests only the new records, and folds them alongside the old ones")]
    public async Task RefreshElementStatsAsync_SecondRefreshWithNewRecords_RequestsOnlyTheNewRecordsAndFoldsBoth()
    {
        var firstEntry = DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "join");
        var secondEntry = DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, elementId: "task");
        var journal = new RecordingWorkflowInstanceService(
            new PagedListResponse<WorkflowExecutionLogRecord> { Items = [firstEntry] },
            new PagedListResponse<WorkflowExecutionLogRecord> { Items = [secondEntry] });
        var sink = new RecordingSinkDesigner();
        var wrapper = CreateWrapper(journal, sink, workflowInstanceId: "instance-1");

        await wrapper.RefreshElementStatsAsync();
        await wrapper.RefreshElementStatsAsync();

        Assert.Equal(2, journal.Calls.Count);
        Assert.Equal(0, journal.Calls[0].Skip);
        Assert.Equal(1, journal.Calls[1].Skip);
        Assert.True(sink.ReceivedStats!["join"].Blocked);
        Assert.Equal(1, sink.ReceivedStats!["task"].Started);
    }

    [Fact(DisplayName = "A refresh after the displayed instance changes starts over, rather than carrying over the previous instance's high-water mark or stats")]
    public async Task RefreshElementStatsAsync_InstanceChanges_StartsOver()
    {
        var firstInstanceEntry = DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "join");
        var secondInstanceEntry = DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, elementId: "task");
        var journal = new RecordingWorkflowInstanceService(
            new PagedListResponse<WorkflowExecutionLogRecord> { Items = [firstInstanceEntry] },
            new PagedListResponse<WorkflowExecutionLogRecord> { Items = [secondInstanceEntry] });
        var sink = new RecordingSinkDesigner();
        var wrapper = CreateWrapper(journal, sink, workflowInstanceId: "instance-1");

        await wrapper.RefreshElementStatsAsync();

        typeof(DiagramDesignerWrapper)
            .GetProperty(nameof(DiagramDesignerWrapper.WorkflowInstanceId))!
            .SetValue(wrapper, "instance-2");
        await wrapper.RefreshElementStatsAsync();

        Assert.Equal(2, journal.Calls.Count);
        Assert.Equal(0, journal.Calls[0].Skip);
        Assert.Equal(0, journal.Calls[1].Skip);
        Assert.DoesNotContain("join", sink.ReceivedStats!.Keys);
        Assert.Equal(1, sink.ReceivedStats!["task"].Started);
    }

    private static WorkflowExecutionLogRecord DiagnosticEntry(string kind, string? elementId = null, string? flowId = null) => new(
        Id: Guid.NewGuid().ToString(),
        ActivityInstanceId: "instance-1",
        ParentActivityInstanceId: null,
        ActivityId: "bpmn-outer",
        ActivityType: BpmnProcessConstants.ActivityTypeName,
        ActivityTypeVersion: 1,
        ActivityName: null,
        NodeId: "bpmn-outer-node",
        Timestamp: DateTimeOffset.UtcNow,
        Sequence: 1,
        EventName: kind,
        Message: null,
        Source: BpmnDiagnosticEventNames.Source,
        ActivityState: null,
        Payload: new BpmnDiagnosticLogPayload(Guid.NewGuid().ToString(), elementId, flowId, null, kind, new Dictionary<string, string>()));

    /// <summary>
    /// Constructs a bare <see cref="DiagramDesignerWrapper"/> with its private <c>WorkflowInstanceService</c> and
    /// <c>_activityGraph</c>/<c>_diagramDesigner</c> fields set through reflection, mirroring the
    /// <c>SetDesigner</c>-style helpers <c>WorkflowInstanceDesignerDisconnectRefreshTests</c> already uses for the
    /// same reason: none of this depends on the component ever being rendered.
    /// </summary>
    private static DiagramDesignerWrapper CreateWrapper(
        IWorkflowInstanceService journal,
        IDiagramDesigner designer,
        string? workflowInstanceId,
        bool includeBpmnProcess = true)
    {
        var wrapper = new DiagramDesignerWrapper();

        typeof(DiagramDesignerWrapper)
            .GetProperty(nameof(DiagramDesignerWrapper.WorkflowInstanceId))!
            .SetValue(wrapper, workflowInstanceId);

        typeof(DiagramDesignerWrapper)
            .GetProperty("WorkflowInstanceService", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(wrapper, journal);

        typeof(DiagramDesignerWrapper)
            .GetField("_diagramDesigner", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(wrapper, designer);

        var graph = BuildActivityGraph(includeBpmnProcess);
        typeof(DiagramDesignerWrapper)
            .GetField("_activityGraph", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(wrapper, graph);

        return wrapper;
    }

    private static ActivityGraph BuildActivityGraph(bool includeBpmnProcess)
    {
        var root = Activity("root", "Elsa.Flowchart");
        var rootNode = new ActivityNode(root);

        if (includeBpmnProcess)
        {
            var outer = Activity("bpmn-outer", BpmnProcessConstants.ActivityTypeName);
            var inner = Activity("bpmn-inner", BpmnProcessConstants.ActivityTypeName);
            var outerNode = new ActivityNode(outer);
            var innerNode = new ActivityNode(inner);

            outerNode.Children.Add(innerNode);
            innerNode.Parents.Add(outerNode);
            rootNode.Children.Add(outerNode);
            outerNode.Parents.Add(rootNode);
        }

        var graph = new ActivityGraph(root, new FixedActivityVisitor(rootNode));
        graph.IndexAsync().GetAwaiter().GetResult();
        return graph;
    }

    private static JsonObject Activity(string id, string typeName) => new()
    {
        ["id"] = id,
        ["nodeId"] = $"{id}-node",
        ["type"] = typeName
    };

    private sealed class FixedActivityVisitor(ActivityNode root) : IActivityVisitor
    {
        public Task<ActivityNode> VisitAsync(JsonObject activity, CancellationToken cancellationToken = default) => Task.FromResult(root);
    }

    /// <summary>A diagram designer that accepts the element-keyed overlay and records what it was given.</summary>
    private sealed class RecordingSinkDesigner : IDiagramDesigner, IBpmnElementStatsSink
    {
        public IReadOnlyDictionary<string, BpmnElementStats>? ReceivedStats { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task UpdateElementStatsAsync(IReadOnlyDictionary<string, BpmnElementStats> elementStats)
        {
            ReceivedStats = elementStats;
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => throw new NotSupportedException();
        public Task UpdateActivityAsync(string id, JsonObject activity) => throw new NotSupportedException();
        public Task UpdateActivityStatsAsync(string id, ActivityStats stats) => throw new NotSupportedException();
        public Task SelectActivityAsync(string id) => throw new NotSupportedException();
        public Task<JsonObject> ReadRootActivityAsync() => throw new NotSupportedException();
        public RenderFragment DisplayDesigner(DisplayContext context) => throw new NotSupportedException();
    }

    /// <summary>An ordinary diagram designer -- a flowchart's, say -- that does not accept an element-keyed overlay.</summary>
    private sealed class NonSinkDesigner : IDiagramDesigner
    {
        public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => throw new NotSupportedException();
        public Task UpdateActivityAsync(string id, JsonObject activity) => throw new NotSupportedException();
        public Task UpdateActivityStatsAsync(string id, ActivityStats stats) => throw new NotSupportedException();
        public Task SelectActivityAsync(string id) => throw new NotSupportedException();
        public Task<JsonObject> ReadRootActivityAsync() => throw new NotSupportedException();
        public RenderFragment DisplayDesigner(DisplayContext context) => throw new NotSupportedException();
    }

    private sealed class RecordingWorkflowInstanceService : IWorkflowInstanceService
    {
        private readonly Queue<PagedListResponse<WorkflowExecutionLogRecord>> _pages;

        public RecordingWorkflowInstanceService(params PagedListResponse<WorkflowExecutionLogRecord>[] pages) => _pages = new(pages);

        public List<(JournalFilter? Filter, int? Skip, int? Take)> Calls { get; } = [];

        public Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(
            string instanceId,
            JournalFilter? filter = null,
            int? skip = null,
            int? take = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((filter, skip, take));

            var page = _pages.Count > 0
                ? _pages.Dequeue()
                : new PagedListResponse<WorkflowExecutionLogRecord> { Items = [] };

            return Task.FromResult(page);
        }

        public Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CancelAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task BulkCancelAsync(BulkCancelWorkflowInstancesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowInstance?> GetAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> BulkExportAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BulkImportAsync(IEnumerable<StreamPart> streamParts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
