using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers a path segment shape the instance viewer's journal drives <see cref="DiagramDesignerWrapper.SelectActivityByActivityIdAsync"/>
/// with: a segment whose container is a <c>BpmnProcess</c> reached through its own collection of activities rather
/// than a named embedded port. Elsa-core's path-segments endpoint (<c>Elsa.Workflows.Api</c>'s
/// <c>WorkflowDefinitions/Graph/Segments</c> endpoint) reports that hop with <c>PortName</c> set to the reflected
/// property name of the container's own activity list -- <c>"Activities"</c> -- because <see cref="Container"/>
/// exposes its children through that collection property rather than through a single named port such as
/// <c>Then</c> or <c>Else</c>. No activity descriptor declares a port by that name, so resolving it as an ordinary
/// embedded port throws before the segment can ever be turned into a container to display or a breadcrumb to draw.
/// </summary>
public sealed class BreadcrumbCollectionPortSegmentTests : BunitContext, IAsyncLifetime
{
    private const string WorkflowDefinitionVersionId = "version-1";
    private const string FlowchartNodeId = "Workflow1:flowchart-1";
    private const string ProcessNodeId = "Workflow1:flowchart-1:process-1";
    private const string NestedChildNodeId = "Workflow1:flowchart-1:process-1:nested-child";

    public BreadcrumbCollectionPortSegmentTests()
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
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    /// <summary>
    /// Reproduces the shape reported as a suspected defect: selecting a leaf activity nested inside a
    /// <c>BpmnProcess</c> container by activity id, the way the workflow instance viewer's journal does, when the
    /// leaf is not in the currently displayed container and has to be resolved through a path segment fetched from
    /// the backend. Before the fix, this throws -- either an <c>InvalidOperationException</c> from
    /// <c>Enumerable.First</c> finding no port named <c>"Activities"</c>, or from casting the container's activity
    /// list to a single embedded activity. After the fix, the process becomes the displayed container and the
    /// breadcrumb falls back to the process's own display name rather than a "displayName: portName" pairing that
    /// no port justifies.
    /// </summary>
    [Fact]
    public async Task SelectingAnActivityNestedInsideABpmnProcessContainer_DoesNotThrow_AndFallsBackToTheContainersDisplayName()
    {
        const string BindingRef = "node-nested-child";
        const string ElementId = "NestedChild_1";

        var nestedChild = CreateWriteLine("nested-child", NestedChildNodeId);
        var process = CreateBpmnProcess("process-1", ProcessNodeId, "Order Process", new JsonArray { nestedChild }, BindingRef, ElementId);
        var root = CreateFlowchartWithContainer(process);

        var pathSegmentsResponse = new GetPathSegmentsResponse(
            new ActivityNode(nestedChild),
            new ActivityNode(process),
            new List<ActivityPathSegment>
            {
                new(process.GetNodeId(), process.GetId(), process.GetTypeName(), "Activities")
            });

        Services.AddSingleton<IWorkflowDefinitionService>(new StubWorkflowDefinitionService(
            WorkflowDefinitionVersionId,
            NestedChildNodeId,
            pathSegmentsResponse));

        Render<MudPopoverProvider>();
        var cut = Render<DiagramDesignerWrapper>(parameters => parameters
            .Add(x => x.WorkflowDefinitionVersionId, WorkflowDefinitionVersionId)
            .Add(x => x.Activity, root)
            .Add(x => x.WorkflowDefinition, new WorkflowDefinition
            {
                Id = WorkflowDefinitionVersionId,
                DefinitionId = "definition-1",
                Name = "Composed",
                Root = root
            }));

        await cut.InvokeAsync(() => cut.Instance.SelectActivityByActivityIdAsync("nested-child", NestedChildNodeId));

        var bpmnDesigner = cut.FindComponent<BpmnDesignerWrapper>();
        Assert.Equal("process-1", bpmnDesigner.Instance.Activity.GetId());
        Assert.Equal(["Root", "Order Process"], BreadcrumbTextsOf(cut));

        // The observable selection: the BPMN canvas is told, through its JS interop, to select the element bound to
        // "nested-child" -- not merely that the container displaying it happens to be right.
        var selectInvocation = Assert.Single(JSInterop.Invocations["selectBpmnElement"]);
        Assert.Equal(ElementId, selectInvocation.Arguments[1]);
    }

    private static IReadOnlyList<string> BreadcrumbTextsOf(IRenderedComponent<DiagramDesignerWrapper> cut) =>
        cut.FindAll("li.mud-breadcrumb-item").Select(x => x.TextContent.Trim()).ToList();

    private static JsonObject CreateFlowchartWithContainer(JsonObject container) => new()
    {
        ["id"] = "flowchart-1",
        ["nodeId"] = FlowchartNodeId,
        ["type"] = "Elsa.Flowchart",
        ["version"] = 1,
        ["activities"] = new JsonArray
        {
            CreateWriteLine("before", $"{FlowchartNodeId}:before"),
            container,
            CreateWriteLine("after", $"{FlowchartNodeId}:after")
        },
        ["connections"] = new JsonArray
        {
            CreateConnection("before", "Done", container.GetId()),
            CreateConnection(container.GetId(), BpmnProcessConstants.DoneOutcomeName, "after")
        }
    };

    /// <summary>
    /// A <c>BpmnProcess</c> with a minimal <c>process</c> payload binding one child activity to a BPMN element, so
    /// that selecting the child by activity id has an observable JS selection to assert on -- rather than the empty
    /// scope the BPMN designer renders as "no content yet", which would leave a selection call silently doing
    /// nothing.
    /// </summary>
    private static JsonObject CreateBpmnProcess(string id, string nodeId, string name, JsonArray activities, string bindingRef, string elementId) => new()
    {
        ["id"] = id,
        ["nodeId"] = nodeId,
        ["name"] = name,
        ["type"] = BpmnProcessConstants.ActivityTypeName,
        ["version"] = 1,
        ["customProperties"] = new JsonObject
        {
            ["canStartWorkflow"] = false
        },
        ["process"] = new JsonObject
        {
            ["processId"] = "order-process",
            ["name"] = name,
            ["isExecutable"] = true,
            ["elements"] = new JsonArray
            {
                new JsonObject { ["elementId"] = "StartEvent_1", ["elementType"] = "startEvent" },
                new JsonObject { ["elementId"] = elementId, ["elementType"] = "task", ["name"] = "nested-child", ["bindingRef"] = bindingRef },
                new JsonObject { ["elementId"] = "EndEvent_1", ["elementType"] = "endEvent" }
            },
            ["sequenceFlows"] = new JsonArray
            {
                new JsonObject { ["flowId"] = "Flow_1", ["sourceRef"] = "StartEvent_1", ["targetRef"] = elementId },
                new JsonObject { ["flowId"] = "Flow_2", ["sourceRef"] = elementId, ["targetRef"] = "EndEvent_1" }
            }
        },
        ["workBindings"] = new JsonObject
        {
            [bindingRef] = "nested-child"
        },
        ["activities"] = activities
    };

    private static JsonObject CreateWriteLine(string id, string nodeId) => new()
    {
        ["id"] = id,
        ["nodeId"] = nodeId,
        ["name"] = id,
        ["type"] = "Elsa.WriteLine",
        ["version"] = 1,
        ["customProperties"] = new JsonObject
        {
            ["canStartWorkflow"] = false
        }
    };

    private static JsonObject CreateConnection(string sourceId, string sourcePort, string targetId) => new()
    {
        ["source"] = new JsonObject { ["activity"] = sourceId, ["port"] = sourcePort },
        ["target"] = new JsonObject { ["activity"] = targetId, ["port"] = "In" }
    };

    /// <summary>
    /// Answers <see cref="IWorkflowDefinitionService.GetPathSegmentsAsync"/> with a canned response, and throws on
    /// every other member -- the same shape as <see cref="ThrowingWorkflowDefinitionServiceBase"/>'s other stubs, so
    /// an unexpected backend call fails loudly rather than returning something the test never configured.
    /// </summary>
    private sealed class StubWorkflowDefinitionService(string expectedId, string expectedChildNodeId, GetPathSegmentsResponse response)
        : ThrowingWorkflowDefinitionServiceBase
    {
        public override Task<GetPathSegmentsResponse?> GetPathSegmentsAsync(string id, string? childNodeId = null, CancellationToken cancellationToken = default)
        {
            Assert.Equal(expectedId, id);
            Assert.Equal(expectedChildNodeId, childNodeId);
            return Task.FromResult<GetPathSegmentsResponse?>(response);
        }
    }

    private sealed class TestActivityRegistry : IActivityRegistry
    {
        private readonly IReadOnlyDictionary<string, ActivityDescriptor> _descriptors = new Dictionary<string, ActivityDescriptor>
        {
            ["Elsa.Flowchart"] = CreateDescriptor("Elsa.Flowchart", "Flowchart", isContainer: true),
            [BpmnProcessConstants.ActivityTypeName] = CreateDescriptor(BpmnProcessConstants.ActivityTypeName, "BPMN Process", isContainer: true),
            ["Elsa.WriteLine"] = CreateDescriptor("Elsa.WriteLine", "Write Line")
        };

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => _descriptors.Values;
        public ActivityDescriptor? Find(string activityType, int? version = null) => _descriptors.GetValueOrDefault(activityType);
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => _descriptors.TryGetValue(activityType, out var descriptor) ? [descriptor] : [];

        public void MarkStale()
        {
        }

        private static ActivityDescriptor CreateDescriptor(string typeName, string displayName, bool isContainer = false) => new()
        {
            TypeName = typeName,
            Name = typeName,
            DisplayName = displayName,
            Version = 1,
            IsBrowsable = true,
            IsContainer = isContainer
        };
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
