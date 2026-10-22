using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Bpmn.Components;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Bpmn.Extensions;
using Elsa.Studio.Bpmn.Tests.Support;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.UI.Contexts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Xunit;
using static Elsa.Studio.Workflows.Tests.Support.BpmnProcessFixtures;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnViewerCanvas"/>: the enabled BPMN designer shows an imported workflow's document read-only in
/// the viewers, with each element's instance state as an overlay, and reports a selected element as its activity.
/// </summary>
public sealed class BpmnViewerCanvasTests : BunitContext, IAsyncLifetime
{
    private const string SourceXml = "<definitions/>";

    private readonly FakeJsRuntime _js = new();
    private readonly FakeJsObject _designer = new();
    private readonly List<JsonObject> _selected = [];

    public BpmnViewerCanvasTests()
    {
        _js.HostModule.Returns("createDesigner", _designer);
        _designer.Returns("load", new BpmnDesignerLoadResult(true, null, []));
        _designer.Returns("select", true);
        Services.AddMudServices();
        Services.AddSingleton<IJSRuntime>(_js);
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new DesignerOptions()));
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact(DisplayName = "The document is shown read-only, with the first stats as overlays on the bound elements")]
    public void ShowsTheDocumentWithItsState()
    {
        EnableDesigner();

        RenderViewer(new Dictionary<string, ActivityStats> { [RootId + ":" + NotifyActivityId] = new() { Started = 1, Completed = 1 } });

        Assert.Equal(true, Assert.Single(_js.HostModule.Calls).Args[4]);
        Assert.Equal([SourceXml], Assert.Single(_designer.Calls, call => call.Identifier == "load").Args);
        Assert.Equal(new BpmnDesignerElementStats { Started = 1, Completed = 1, Active = 0, Blocked = false, Faulted = false }, LastOverlay()["NotifyWarehouse"]);
    }

    [Fact(DisplayName = "State that arrives while the document loads is shown once it has loaded")]
    public async Task StateDuringTheLoad_IsShownAfterIt()
    {
        EnableDesigner();
        var load = new TaskCompletionSource<object?>();
        _designer.Handlers["load"] = _ => load.Task;
        var cut = Render<BpmnViewerCanvas>(parameters => parameters.Add(x => x.Activity, OrderProcess()).Add(x => x.SourceXml, SourceXml));
        cut.WaitForAssertion(() => Assert.Contains(_designer.Calls, call => call.Identifier == "load"));

        await cut.InvokeAsync(() => cut.Instance.UpdateElementStatsAsync(new Dictionary<string, BpmnElementStats> { ["Gateway_1"] = new() { Completed = 1 } }));
        await cut.InvokeAsync(() => cut.Instance.UpdateActivityStatsAsync(ShipActivityId, new() { Started = 1, Uncompleted = 1 }));
        Assert.DoesNotContain(_designer.Calls, call => call.Identifier == "applyOverlays");

        await cut.InvokeAsync(() => load.SetResult(new BpmnDesignerLoadResult(true, null, [])));

        cut.WaitForAssertion(() => Assert.Equal(["Gateway_1", "Ship"], LastOverlay().Keys.Order()));
        Assert.Single(_designer.Calls, call => call.Identifier == "applyOverlays");
    }

    [Fact(DisplayName = "A newer document waits for the load in flight, which then shows no state; the newer one does")]
    public async Task AStaleLoad_ShowsNothing()
    {
        EnableDesigner();
        var loads = new Queue<TaskCompletionSource<object?>>([new(), new()]);
        var pending = new List<TaskCompletionSource<object?>>();
        _designer.Handlers["load"] = _ =>
        {
            var load = loads.Dequeue();
            pending.Add(load);
            return load.Task;
        };
        var cut = Render<BpmnViewerCanvas>(parameters => parameters.Add(x => x.Activity, OrderProcess()).Add(x => x.SourceXml, SourceXml));
        cut.WaitForAssertion(() => Assert.Single(pending));

        var newer = cut.InvokeAsync(() => cut.Instance.LoadBpmnAsync(OrderProcess(), "<newer/>", null));
        Assert.Single(pending);
        await cut.InvokeAsync(() => pending[0].SetResult(new BpmnDesignerLoadResult(true, null, [])));
        cut.WaitForAssertion(() => Assert.Equal(2, pending.Count));
        Assert.DoesNotContain(_designer.Calls, call => call.Identifier == "applyOverlays");

        await cut.InvokeAsync(() => pending[1].SetResult(new BpmnDesignerLoadResult(true, null, [])));
        await newer;

        Assert.Single(_designer.Calls, call => call.Identifier == "applyOverlays");
        Assert.Equal(["<newer/>"], _designer.Calls.Last(call => call.Identifier == "load").Args);
    }

    [Fact(DisplayName = "After a new document loads, selecting an activity selects its element again")]
    public async Task ANewDocument_ForgetsTheSelection()
    {
        EnableDesigner();
        var cut = RenderViewer();
        var host = cut.FindComponent<BpmnCanvasHost>().Instance;
        await cut.InvokeAsync(() => cut.Instance.SelectActivityAsync(NotifyActivityId));
        await cut.InvokeAsync(() => host.OnDesignerSelectionChanged(new(["NotifyWarehouse"], new("NotifyWarehouse", "bpmn:ServiceTask", null))));

        await cut.InvokeAsync(() => cut.Instance.LoadBpmnAsync(OrderProcess(), "<newer/>", null));
        await cut.InvokeAsync(() => cut.Instance.SelectActivityAsync(NotifyActivityId));

        Assert.Equal(2, _designer.Calls.Count(call => call.Identifier == "select"));
    }

    [Fact(DisplayName = "An activity selected while the document loads is selected once it has loaded")]
    public async Task ASelectionDuringTheLoad_IsMadeAfterIt()
    {
        EnableDesigner();
        var load = new TaskCompletionSource<object?>();
        _designer.Handlers["load"] = _ => load.Task;
        var cut = Render<BpmnViewerCanvas>(parameters => parameters.Add(x => x.Activity, OrderProcess()).Add(x => x.SourceXml, SourceXml));
        cut.WaitForAssertion(() => Assert.Contains(_designer.Calls, call => call.Identifier == "load"));

        await cut.InvokeAsync(() => cut.Instance.SelectActivityAsync(NotifyActivityId));
        Assert.DoesNotContain(_designer.Calls, call => call.Identifier == "select");

        await cut.InvokeAsync(() => load.SetResult(new BpmnDesignerLoadResult(true, null, [])));

        cut.WaitForAssertion(() => Assert.Equal(["NotifyWarehouse"], Assert.Single(_designer.Calls, call => call.Identifier == "select").Args));
    }

    [Fact(DisplayName = "A document the designer cannot show says why, and is loaded again when asked for again")]
    public async Task AFailedLoad_IsShownAndRetried()
    {
        EnableDesigner();
        _designer.Returns("load", new BpmnDesignerLoadResult(false, "The document is not valid BPMN.", []));
        var cut = Render<BpmnViewerCanvas>(parameters => parameters.Add(x => x.Activity, OrderProcess()).Add(x => x.SourceXml, SourceXml));

        cut.WaitForAssertion(() => Assert.Contains("The document is not valid BPMN.", cut.Find("[data-testid=bpmn-viewer-load-error]").TextContent));
        Assert.DoesNotContain(_designer.Calls, call => call.Identifier == "applyOverlays");

        _designer.Returns("load", new BpmnDesignerLoadResult(true, null, []));
        await cut.InvokeAsync(() => cut.Instance.LoadBpmnAsync(OrderProcess(), SourceXml, null));

        Assert.Equal(2, _designer.Calls.Count(call => call.Identifier == "load"));
        Assert.Empty(cut.FindAll("[data-testid=bpmn-viewer-load-error]"));
        Assert.Single(_designer.Calls, call => call.Identifier == "applyOverlays");
    }

    [Fact(DisplayName = "A document whose load failed is loaded again when asked for again")]
    public async Task AThrownLoad_IsRetried()
    {
        EnableDesigner();
        _designer.Handlers["load"] = _ => Task.FromException<object?>(new JSException("The connection was lost."));
        var cut = Render<BpmnViewerCanvas>(parameters => parameters.Add(x => x.Activity, OrderProcess()).Add(x => x.SourceXml, SourceXml));
        cut.WaitForAssertion(() => Assert.Contains(_designer.Calls, call => call.Identifier == "load"));

        _designer.Returns("load", new BpmnDesignerLoadResult(true, null, []));
        await cut.InvokeAsync(() => cut.Instance.LoadBpmnAsync(OrderProcess(), SourceXml, null));

        Assert.Equal(2, _designer.Calls.Count(call => call.Identifier == "load"));
        Assert.Single(_designer.Calls, call => call.Identifier == "applyOverlays");
    }

    [Fact(DisplayName = "A selected element is reported as its bound activity, an unbound one as its scope, and no selection as the root")]
    public async Task ReportsSelectionAsActivities()
    {
        EnableDesigner();
        var cut = RenderViewer();
        var host = cut.FindComponent<BpmnCanvasHost>().Instance;

        await cut.InvokeAsync(() => host.OnDesignerSelectionChanged(new(["Ship"], new("Ship", "bpmn:Task", null))));
        await cut.InvokeAsync(() => host.OnDesignerSelectionChanged(new(["Shipped"], new("Shipped", "bpmn:EndEvent", null))));
        await cut.InvokeAsync(() => host.OnDesignerSelectionChanged(new([], null)));

        Assert.Equal([ShipActivityId, SubprocessId, RootId], _selected.Select(activity => activity["id"]!.GetValue<string>()));
    }

    [Fact(DisplayName = "Selecting an activity selects its element, and the designer's echo of it is not reported back")]
    public async Task SelectsTheBoundElementWithoutEcho()
    {
        EnableDesigner();
        var cut = RenderViewer();
        var host = cut.FindComponent<BpmnCanvasHost>().Instance;

        await cut.InvokeAsync(() => cut.Instance.SelectActivityAsync(NotifyActivityId));
        await cut.InvokeAsync(() => host.OnDesignerSelectionChanged(new(["NotifyWarehouse"], new("NotifyWarehouse", "bpmn:ServiceTask", null))));
        await cut.InvokeAsync(() => cut.Instance.SelectActivityAsync(RootId));
        await cut.InvokeAsync(() => host.OnDesignerSelectionChanged(new(["Gateway_1"], new("Gateway_1", "bpmn:ExclusiveGateway", null))));

        Assert.Equal(["NotifyWarehouse"], Assert.Single(_designer.Calls, call => call.Identifier == "select").Args);
        Assert.Equal([RootId], _selected.Select(activity => activity["id"]!.GetValue<string>()));
    }

    [Fact(DisplayName = "With a designer enabled, a workflow's source document is shown in it, and a scope without one is not")]
    public void WithADesigner_TheSourceDocumentIsShownInIt()
    {
        EnableDesigner();

        Assert.NotEmpty(RenderDiagram(OrderProcess(), SourceXml).FindComponents<BpmnViewerCanvas>());
        Assert.Empty(RenderDiagram(EmptyScope(), sourceXml: null).FindComponents<BpmnViewerCanvas>());
    }

    [Fact(DisplayName = "Without a designer, a workflow's source document stays on the read-only canvas")]
    public void WithoutADesigner_TheReadOnlyCanvasStays()
    {
        Services.AddBpmnDesignerResolver();

        var cut = RenderDiagram(EmptyScope(), SourceXml);

        Assert.Empty(cut.FindComponents<BpmnViewerCanvas>());
        Assert.Contains("has no content yet", cut.Markup);
    }

    [Fact(DisplayName = "With a designer enabled, the source document's nested scopes are shown along with the root")]
    public void WithADesigner_NestedScopesAreShown()
    {
        EnableDesigner();
        var designer = NewDesigner(Services.GetRequiredService<BpmnDesignerResolver>());

        Assert.True(designer.ShowsNestedScopes(OrderProcess(), DefinitionWith(SourceXml)));
        Assert.False(designer.ShowsNestedScopes(OrderProcess(), DefinitionWith(sourceXml: null)));
        Assert.False(NewDesigner().ShowsNestedScopes(OrderProcess(), DefinitionWith(SourceXml)));
    }

    private void EnableDesigner() => Services.AddBpmnDesigner(new BpmnDesignerDescriptor("test-designer", "_content/Test/designer.js"));

    private IRenderedComponent<BpmnViewerCanvas> RenderViewer(IDictionary<string, ActivityStats>? stats = null)
    {
        var cut = Render<BpmnViewerCanvas>(parameters => parameters
            .Add(x => x.Activity, OrderProcess())
            .Add(x => x.SourceXml, SourceXml)
            .Add(x => x.ActivityStats, stats)
            .Add(x => x.ActivitySelected, activity => _selected.Add(activity)));
        cut.WaitForAssertion(() => Assert.Contains(_designer.Calls, call => call.Identifier == "applyOverlays"));
        return cut;
    }

    private IRenderedComponent<RenderFragmentHost> RenderDiagram(JsonObject root, string? sourceXml)
    {
        var definition = DefinitionWith(sourceXml);
        return Render<RenderFragmentHost>(parameters => parameters.Add(x => x.Content, NewDesigner().DisplayDesigner(new DisplayContext(root) { WorkflowDefinition = definition })));
    }

    private static WorkflowDefinition DefinitionWith(string? sourceXml)
    {
        var definition = new WorkflowDefinition();

        if (sourceXml != null)
        {
            definition.CustomProperties[BpmnProcessConstants.SourceXmlCustomPropertyKey] = sourceXml;
        }

        return definition;
    }

    private IReadOnlyDictionary<string, BpmnDesignerElementStats> LastOverlay() =>
        (IReadOnlyDictionary<string, BpmnDesignerElementStats>)_designer.Calls.Last(call => call.Identifier == "applyOverlays").Args[0]!;

    private static JsonObject EmptyScope() => new() { ["id"] = "Process_1", ["type"] = "Elsa.BpmnProcess" };

    private static BpmnDiagramDesigner NewDesigner(BpmnDesignerResolver? designerResolver = null) =>
        new(new TestLocalizer(), Microsoft.Extensions.Options.Options.Create(new DesignerOptions()), null!, null!, null!, null!, designerResolver);

    /// <summary>Renders a fragment, as the diagram designer wrapper renders a designer's display.</summary>
    private sealed class RenderFragmentHost : ComponentBase
    {
        [Parameter] public RenderFragment Content { get; set; } = null!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) => builder.AddContent(0, Content);
    }
}
