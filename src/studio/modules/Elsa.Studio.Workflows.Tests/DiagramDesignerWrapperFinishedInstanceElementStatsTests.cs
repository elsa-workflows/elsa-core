using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the case the mount race hid completely: a <b>finished</b> workflow instance never gets a
/// <see cref="Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components.WorkflowInstanceDesigner"/>-driven
/// observer tick (<c>UpdateObserverAsync</c> only creates one while the instance is running), so the one
/// unconditional refresh <see cref="DiagramDesignerWrapper"/> already performs on load -- before it has ever
/// rendered its <see cref="BpmnDesignerWrapper"/> -- is the only chance the overlay ever gets to reach the canvas.
/// </summary>
public sealed class DiagramDesignerWrapperFinishedInstanceElementStatsTests : BunitContext, IAsyncLifetime
{
    private const string ElementId = "join";

    public DiagramDesignerWrapperFinishedInstanceElementStatsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<BpmnDiagnostic[]>("loadBpmnDiagram", _ => true).SetResult([]);
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry, TestActivityRegistry>();
        Services.AddSingleton<IDomAccessor, NoOpDomAccessor>();
        Services.AddSingleton<IActivityExecutionService>(new StubActivityExecutionService());
        Services.AddSingleton<IWorkflowInstanceService>(new StubWorkflowInstanceService(JournalEntry()));
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void OpeningAFinishedInstance_RefreshesTheElementOverlayExactlyOnce_WithNoObserverTicksAtAll()
    {
        var handler = JSInterop.SetupVoid("updateBpmnElementStats", _ => true);
        var root = CreateBpmnRoot();

        Render<MudPopoverProvider>();
        Render<DiagramDesignerWrapper>(parameters => parameters
            .Add(x => x.WorkflowDefinitionVersionId, "version-1")
            .Add(x => x.Activity, root)
            .Add(x => x.WorkflowDefinition, CreateDefinition(root))
            .Add(x => x.WorkflowInstanceId, "instance-1"));

        var invocation = Assert.Single(handler.Invocations);
        var appliedStats = Assert.IsType<Dictionary<string, BpmnElementStats>>(invocation.Arguments[1]);
        Assert.True(appliedStats[ElementId].Blocked);
    }

    private static WorkflowExecutionLogRecord JournalEntry() => new(
        Id: Guid.NewGuid().ToString(),
        ActivityInstanceId: "instance-1",
        ParentActivityInstanceId: null,
        ActivityId: "order-process",
        ActivityType: BpmnProcessConstants.ActivityTypeName,
        ActivityTypeVersion: 1,
        ActivityName: null,
        NodeId: "Workflow1:order-process",
        Timestamp: DateTimeOffset.UtcNow,
        Sequence: 1,
        EventName: BpmnDiagnosticEventNames.Waiting,
        Message: null,
        Source: BpmnDiagnosticEventNames.Source,
        ActivityState: null,
        Payload: new BpmnDiagnosticLogPayload(Guid.NewGuid().ToString(), ElementId, null, null, BpmnDiagnosticEventNames.Waiting, new Dictionary<string, string>()));

    private static JsonObject CreateBpmnRoot() => new()
    {
        ["id"] = "order-process",
        ["nodeId"] = "Workflow1:order-process",
        ["name"] = "Order Process",
        ["type"] = BpmnProcessConstants.ActivityTypeName,
        ["version"] = 1,
        ["customProperties"] = new JsonObject { ["canStartWorkflow"] = true },
        ["process"] = new JsonObject
        {
            ["processId"] = "order-process",
            ["name"] = "Order Process",
            ["isExecutable"] = true,
            ["elements"] = new JsonArray
            {
                new JsonObject { ["elementId"] = "StartEvent_1", ["elementType"] = "startEvent" },
                new JsonObject { ["elementId"] = ElementId, ["elementType"] = "parallelGateway" },
                new JsonObject { ["elementId"] = "EndEvent_1", ["elementType"] = "endEvent" }
            },
            ["sequenceFlows"] = new JsonArray()
        },
        ["workBindings"] = new JsonObject(),
        ["activities"] = new JsonArray()
    };

    private static WorkflowDefinition CreateDefinition(JsonObject root) => new()
    {
        Id = "version-1",
        DefinitionId = "definition-1",
        Name = "Order Process",
        Root = root
    };

    private sealed class StubActivityExecutionService : IActivityExecutionService
    {
        public Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ActivityExecutionReport([]));

        public Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ActivityExecutionCallStack> GetCallStackAsync(string activityExecutionId, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedListResponse<RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubWorkflowInstanceService(params WorkflowExecutionLogRecord[] entries) : IWorkflowInstanceService
    {
        public Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(
            string instanceId,
            JournalFilter? filter = null,
            int? skip = null,
            int? take = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedListResponse<WorkflowExecutionLogRecord> { Items = skip is null or 0 ? entries : [] });

        public Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CancelAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task BulkCancelAsync(BulkCancelWorkflowInstancesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowInstance?> GetAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> BulkExportAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BulkImportAsync(IEnumerable<Refit.StreamPart> streamParts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestActivityRegistry : IActivityRegistry
    {
        private readonly IReadOnlyDictionary<string, ActivityDescriptor> _descriptors = new Dictionary<string, ActivityDescriptor>
        {
            [BpmnProcessConstants.ActivityTypeName] = new()
            {
                TypeName = BpmnProcessConstants.ActivityTypeName,
                Name = BpmnProcessConstants.ActivityTypeName,
                DisplayName = "BPMN Process",
                Version = 1,
                IsBrowsable = true,
                IsContainer = true
            }
        };

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => _descriptors.Values;
        public ActivityDescriptor? Find(string activityType, int? version = null) => _descriptors.GetValueOrDefault(activityType);
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => _descriptors.TryGetValue(activityType, out var descriptor) ? [descriptor] : [];

        public void MarkStale()
        {
        }
    }

    private sealed class NoOpDomAccessor : IDomAccessor
    {
        public Task<DomRect> GetBoundingClientRectAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.FromResult(new DomRect());
        public Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.FromResult(0d);
        public Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
