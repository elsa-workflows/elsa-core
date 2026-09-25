using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
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
/// Covers drilling into a nested <c>Elsa.BpmnProcess</c> scope from the shared
/// <see cref="DiagramDesignerWrapper"/>: from a flowchart that composes one, and from a BPMN root that contains an
/// expanded subprocess. Both raise the scope through the same double-click path and both must land in the BPMN
/// designer with the scope as its root, with a breadcrumb back to where they came from.
/// </summary>
/// <remarks>
/// <para>
/// The invariant these tests exist for is that drilling in and out is a <em>view</em> operation: the nested scope's
/// JSON must come back byte-identical, because <c>customProperties.canStartWorkflow</c> -- what elsa-core's
/// <c>BpmnProcess.IsRootScope</c> is backed by -- decides whether the scope's start events are indexed as ways into
/// the workflow. A nested scope that comes back claiming root position is refused by the applier, and a root scope
/// that comes back denying it silently stops answering the messages it used to answer. Neither shows up on the
/// canvas, which is why the round trip is asserted on the document rather than on what is drawn.
/// </para>
/// <para>
/// The workflow definition service is deliberately a stub that throws: everything these tests exercise resolves out
/// of the graph the wrapper already holds, so a call to the backend would mean the wrapper had lost the node -- a
/// failure that would otherwise show up only as a silent network round trip in production.
/// </para>
/// </remarks>
public sealed class BpmnNestedScopeDrillDownTests : BunitContext, IAsyncLifetime
{
    private const string SourceXmlCustomPropertyKey = "Bpmn:SourceXml";
    private const string SourceXml = """<?xml version="1.0"?><definitions/>""";

    public BpmnNestedScopeDrillDownTests()
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
        Services.AddSingleton<IWorkflowDefinitionService, ThrowingWorkflowDefinitionService>();
        Services.AddSingleton<IDomAccessor, NoOpDomAccessor>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void FlowchartRoot_ShowsTheFlowchartDesigner_AndNoBreadcrumbTrail()
    {
        var cut = RenderWrapper(CreateFlowchartWithNestedProcess());

        Assert.NotNull(cut.FindComponent<FlowchartDesigner>());
        Assert.Empty(cut.FindComponents<BpmnDesignerWrapper>());
        Assert.Empty(BreadcrumbTextsOf(cut));
    }

    [Fact]
    public async Task DoubleClickingANestedProcessOnAFlowchart_OpensTheBpmnDesignerOnThatScope()
    {
        var root = CreateFlowchartWithNestedProcess();
        var cut = RenderWrapper(root);

        await DoubleClickOnFlowchartAsync(cut, NestedProcessOf(root));

        var bpmnDesigner = cut.FindComponent<BpmnDesignerWrapper>();
        Assert.Equal("process-1", bpmnDesigner.Instance.Activity.GetId());
        Assert.Empty(cut.FindComponents<FlowchartDesigner>());
    }

    [Fact]
    public async Task DrillingIntoANestedProcess_ShowsABreadcrumbBackToTheFlowchart()
    {
        var root = CreateFlowchartWithNestedProcess();
        var cut = RenderWrapper(root);

        await DoubleClickOnFlowchartAsync(cut, NestedProcessOf(root));

        Assert.Equal(["Root", "Order Process"], BreadcrumbTextsOf(cut));
    }

    /// <summary>
    /// The scope's geometry lives in the definition's imported source XML, keyed by element id, so a nested scope
    /// needs the very same document a root scope does. Losing it does not fail: the adapter silently falls back to
    /// its own layout, and the diagram merely stops looking like the one that was imported.
    /// </summary>
    [Fact]
    public async Task ANestedScope_StillReceivesTheDefinitionsImportedSourceXml()
    {
        var root = CreateFlowchartWithNestedProcess();
        var cut = RenderWrapper(root);

        await DoubleClickOnFlowchartAsync(cut, NestedProcessOf(root));

        Assert.Equal(SourceXml, cut.FindComponent<BpmnDesignerWrapper>().Instance.SourceXml);
    }

    [Fact]
    public async Task DrillingBackOut_ReturnsToTheFlowchart()
    {
        var root = CreateFlowchartWithNestedProcess();
        var cut = RenderWrapper(root);
        await DoubleClickOnFlowchartAsync(cut, NestedProcessOf(root));

        await ClickRootBreadcrumbAsync(cut);

        Assert.NotNull(cut.FindComponent<FlowchartDesigner>());
        Assert.Empty(cut.FindComponents<BpmnDesignerWrapper>());
        Assert.Empty(BreadcrumbTextsOf(cut));
    }

    /// <summary>
    /// The round trip, including the read-back the editor performs while the nested scope is the displayed
    /// container -- which is where a designer that rebuilt its document from a view model instead of handing back
    /// the one it was given would quietly rewrite the scope.
    /// </summary>
    [Fact]
    public async Task DrillingInAndOut_LeavesTheNestedProcessJsonUnchanged()
    {
        var root = CreateFlowchartWithNestedProcess();
        var before = NestedProcessOf(root).ToJsonString();
        var cut = RenderWrapper(root);
        await DoubleClickOnFlowchartAsync(cut, NestedProcessOf(root));

        await cut.InvokeAsync(() => cut.Instance.GetActivityAsync());
        await ClickRootBreadcrumbAsync(cut);

        var graph = await cut.Instance.GetActivityGraphAsync();
        Assert.Equal(before, NestedProcessOf(graph.Activity).ToJsonString());
    }

    /// <summary>
    /// Named separately from the byte comparison above: a future change that reordered or reformatted the JSON would
    /// break that one for a reason nobody cares about, while this one only breaks if the flag itself moved.
    /// </summary>
    [Fact]
    public async Task DrillingInAndOut_LeavesTheNestedScopeDenyingRootPosition()
    {
        var root = CreateFlowchartWithNestedProcess();
        var cut = RenderWrapper(root);
        await DoubleClickOnFlowchartAsync(cut, NestedProcessOf(root));

        await cut.InvokeAsync(() => cut.Instance.GetActivityAsync());
        await ClickRootBreadcrumbAsync(cut);

        var graph = await cut.Instance.GetActivityGraphAsync();
        Assert.False(NestedProcessOf(graph.Activity)["customProperties"]!["canStartWorkflow"]!.GetValue<bool>());
    }

    /// <summary>
    /// The direction that could be mistaken for success: a designer that <em>did</em> rewrite a scope would still
    /// render, still drill out, and still save -- the workflow would simply start refusing to run, server-side, once
    /// the applier saw a nested scope claiming root position. So the flag is asserted in both directions on one
    /// document: after drilling through it, the subprocess still denies root position <em>and</em> the root scope
    /// still claims it. A designer that normalised the flag either way would break exactly one of the two.
    /// </summary>
    [Fact]
    public async Task DrillingIntoASubprocess_LeavesTheRootScopeClaimingRootPosition()
    {
        var root = CreateBpmnRootWithSubprocess();
        var cut = RenderWrapper(root);

        await DoubleClickOnBpmnAsync(cut, root, NestedProcessOf(root));
        await cut.InvokeAsync(() => cut.Instance.GetActivityAsync());
        await ClickRootBreadcrumbAsync(cut);

        var graph = await cut.Instance.GetActivityGraphAsync();
        Assert.True(graph.Activity["customProperties"]!["canStartWorkflow"]!.GetValue<bool>());
        Assert.False(NestedProcessOf(graph.Activity)["customProperties"]!["canStartWorkflow"]!.GetValue<bool>());
    }

    [Fact]
    public async Task DoubleClickingASubprocessInsideABpmnRoot_RendersTheNestedScopeAsTheDesignersRoot()
    {
        var root = CreateBpmnRootWithSubprocess();
        var cut = RenderWrapper(root);

        await DoubleClickOnBpmnAsync(cut, root, NestedProcessOf(root));

        var bpmnDesigner = cut.FindComponent<BpmnDesignerWrapper>();
        Assert.Equal("subprocess-1", bpmnDesigner.Instance.Activity.GetId());
        Assert.Equal(SourceXml, bpmnDesigner.Instance.SourceXml);
        Assert.Equal(["Root", "Review Subprocess"], BreadcrumbTextsOf(cut));
    }

    /// <summary>
    /// A scope added from the toolbox has no <c>process</c> at all, and importing into a nested node is not
    /// supported server-side, so the only honest thing the designer can do is say so. A blank canvas would be
    /// indistinguishable from a diagram that failed to load.
    /// </summary>
    [Fact]
    public async Task DrillingIntoAnEmptyScope_ShowsTheEmptyStateNoticeRatherThanABlankCanvas()
    {
        var root = CreateFlowchartWithNestedProcess(emptyScope: true);
        var cut = RenderWrapper(root);

        await DoubleClickOnFlowchartAsync(cut, NestedProcessOf(root));

        Assert.NotNull(cut.FindComponent<BpmnDesignerWrapper>());
        Assert.Contains("This BPMN scope has no content yet.", cut.Markup);
    }

    /// <summary>
    /// With no canvas mounted there is nothing to read a document back from, which is the shape of scope most
    /// likely to be handed back as an empty object. It still has to come back exactly as it went in.
    /// </summary>
    [Fact]
    public async Task DrillingIntoAnEmptyScope_StillLeavesItsJsonUnchanged()
    {
        var root = CreateFlowchartWithNestedProcess(emptyScope: true);
        var before = NestedProcessOf(root).ToJsonString();
        var cut = RenderWrapper(root);
        await DoubleClickOnFlowchartAsync(cut, NestedProcessOf(root));

        await cut.InvokeAsync(() => cut.Instance.GetActivityAsync());
        await ClickRootBreadcrumbAsync(cut);

        var graph = await cut.Instance.GetActivityGraphAsync();
        Assert.Equal(before, NestedProcessOf(graph.Activity).ToJsonString());
    }

    private IRenderedComponent<DiagramDesignerWrapper> RenderWrapper(JsonObject root)
    {
        Render<MudPopoverProvider>();
        return Render<DiagramDesignerWrapper>(parameters => parameters
            .Add(x => x.WorkflowDefinitionVersionId, "version-1")
            .Add(x => x.Activity, root)
            .Add(x => x.WorkflowDefinition, CreateDefinition(root)));
    }

    private static async Task DoubleClickOnFlowchartAsync(IRenderedComponent<DiagramDesignerWrapper> cut, JsonObject activity)
    {
        // What the X6 canvas sends: a deserialized copy of the node's activity, not the graph's own instance.
        var designer = cut.FindComponent<FlowchartDesigner>();
        await cut.InvokeAsync(() => designer.Instance.HandleActivityDoubleClick((JsonObject)activity.DeepClone()));
    }

    /// <summary>
    /// What the BPMN canvas actually sends for an expanded subprocess shape: <c>ActivityId</c> is the work bound to
    /// the element -- the nested scope itself -- while <c>ScopeActivityId</c> names the scope the element *lives in*,
    /// which is the root. Getting those two the wrong way round would still resolve to a <c>BpmnProcess</c> here and
    /// would open the wrong one on a real document, so the faithful shape is what is sent.
    /// </summary>
    private static async Task DoubleClickOnBpmnAsync(IRenderedComponent<DiagramDesignerWrapper> cut, JsonObject enclosingScope, JsonObject subprocess)
    {
        var designer = cut.FindComponent<BpmnDesigner>();
        var selection = new BpmnElementSelection(
            ElementId: "SubProcess_1",
            ElementType: "subProcess",
            Kind: "subProcess",
            Name: "Review Subprocess",
            ActivityId: subprocess.GetId(),
            BindingState: "bound",
            ScopeId: "order-process",
            ScopeActivityId: enclosingScope.GetId(),
            BoundaryHostElementId: null,
            ChildScopeId: "review-process");

        await cut.InvokeAsync(() => designer.Instance.HandleActivityDoubleClick(selection));
    }

    private static IReadOnlyList<string> BreadcrumbTextsOf(IRenderedComponent<DiagramDesignerWrapper> cut) =>
        cut.FindAll("li.mud-breadcrumb-item").Select(x => x.TextContent.Trim()).ToList();

    private static async Task ClickRootBreadcrumbAsync(IRenderedComponent<DiagramDesignerWrapper> cut)
    {
        var rootLink = cut.FindAll("a").First(x => x.TextContent.Contains("Root"));
        await cut.InvokeAsync(() => rootLink.Click());
    }

    /// <summary>
    /// The one nested <c>BpmnProcess</c> in <paramref name="container"/> -- the flowchart's composed process, or the
    /// BPMN root's subprocess scope; both are children of the container's <c>activities</c>.
    /// </summary>
    private static JsonObject NestedProcessOf(JsonObject container) =>
        container["activities"]!.AsArray().OfType<JsonObject>().Single(x => x.GetTypeName() == BpmnProcessConstants.ActivityTypeName);

    private static WorkflowDefinition CreateDefinition(JsonObject root) => new()
    {
        Id = "version-1",
        DefinitionId = "definition-1",
        Name = "Composed",
        Root = root,
        CustomProperties = new Dictionary<string, object>
        {
            [SourceXmlCustomPropertyKey] = SourceXml
        }
    };

    /// <summary>
    /// A flowchart that runs a <c>WriteLine</c>, then a BPMN process, then another <c>WriteLine</c> on the process's
    /// <c>Done</c> outcome -- the composition elsa-core's own composition test builds, spelled as the JSON Studio
    /// receives.
    /// </summary>
    private static JsonObject CreateFlowchartWithNestedProcess(bool emptyScope = false)
    {
        var process = new JsonObject
        {
            ["id"] = "process-1",
            ["nodeId"] = "Workflow1:flowchart-1:process-1",
            ["name"] = "Order Process",
            ["type"] = BpmnProcessConstants.ActivityTypeName,
            ["version"] = 1,
            ["customProperties"] = new JsonObject
            {
                ["canStartWorkflow"] = false
            },
            ["workBindings"] = new JsonObject(),
            ["activities"] = new JsonArray()
        };

        if (!emptyScope)
            process["process"] = CreateProcessPayload("order-process", "Order Process");

        return new JsonObject
        {
            ["id"] = "flowchart-1",
            ["nodeId"] = "Workflow1:flowchart-1",
            ["type"] = "Elsa.Flowchart",
            ["version"] = 1,
            ["activities"] = new JsonArray
            {
                CreateWriteLine("before"),
                process,
                CreateWriteLine("after")
            },
            ["connections"] = new JsonArray
            {
                CreateConnection("before", "Done", "process-1"),
                CreateConnection("process-1", BpmnProcessConstants.DoneOutcomeName, "after")
            }
        };
    }

    /// <summary>
    /// A BPMN root scope containing an expanded subprocess, which is itself a nested <c>BpmnProcess</c> bound as
    /// work -- the shape W10 already raises on double-click.
    /// </summary>
    private static JsonObject CreateBpmnRootWithSubprocess() => new()
    {
        ["id"] = "order-process",
        ["nodeId"] = "Workflow1:order-process",
        ["name"] = "Order Process",
        ["type"] = BpmnProcessConstants.ActivityTypeName,
        ["version"] = 1,
        ["customProperties"] = new JsonObject
        {
            ["canStartWorkflow"] = true
        },
        ["process"] = CreateProcessPayload("order-process", "Order Process", new JsonObject
        {
            ["elementId"] = "SubProcess_1",
            ["elementType"] = "subProcess",
            ["name"] = "Review Subprocess",
            ["bindingRef"] = "node-SubProcess_1"
        }),
        ["workBindings"] = new JsonObject
        {
            ["node-SubProcess_1"] = "subprocess-1"
        },
        ["activities"] = new JsonArray
        {
            new JsonObject
            {
                ["id"] = "subprocess-1",
                ["nodeId"] = "Workflow1:order-process:subprocess-1",
                ["name"] = "Review Subprocess",
                ["type"] = BpmnProcessConstants.ActivityTypeName,
                ["version"] = 1,
                ["customProperties"] = new JsonObject
                {
                    ["canStartWorkflow"] = false
                },
                ["process"] = CreateProcessPayload("review-process", "Review Subprocess"),
                ["workBindings"] = new JsonObject(),
                ["activities"] = new JsonArray()
            }
        }
    };

    private static JsonObject CreateProcessPayload(string processId, string name, JsonObject? middleElement = null)
    {
        var elements = new JsonArray { new JsonObject { ["elementId"] = "StartEvent_1", ["elementType"] = "startEvent" } };

        if (middleElement != null)
            elements.Add(middleElement);

        elements.Add(new JsonObject { ["elementId"] = "EndEvent_1", ["elementType"] = "endEvent" });

        return new()
        {
            ["processId"] = processId,
            ["name"] = name,
            ["isExecutable"] = true,
            ["elements"] = elements,
            ["sequenceFlows"] = new JsonArray
            {
                new JsonObject { ["flowId"] = "Flow_1", ["sourceRef"] = "StartEvent_1", ["targetRef"] = "EndEvent_1" }
            }
        };
    }

    private static JsonObject CreateWriteLine(string id) => new()
    {
        ["id"] = id,
        ["nodeId"] = $"Workflow1:flowchart-1:{id}",
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
