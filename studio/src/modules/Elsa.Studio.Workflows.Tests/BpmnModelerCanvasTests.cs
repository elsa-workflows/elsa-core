using System.Text.Json;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Studio.Bpmn.Components;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Bpmn.Extensions;
using Elsa.Studio.Bpmn.Tests.Support;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Tests.Support;
using Elsa.Studio.Workflows.UI.Contexts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnModelerCanvas"/> and how <see cref="BpmnDiagramDesigner"/> mounts it: the canvas loads each
/// revision the session reads into the enabled designer, reports the designer's events to the session, and survives the
/// editor replacing the designer on every reload of the workflow.
/// </summary>
public sealed class BpmnModelerCanvasTests : BunitContext, IAsyncLifetime
{
    private const string StoredXml = "<stored/>";

    private readonly FakeJsRuntime _js = new();
    private readonly FakeJsObject _designer = new();
    private readonly FakeBpmnXmlService _xmlService = new();
    private readonly BpmnModelerSession _session;

    public BpmnModelerCanvasTests()
    {
        _xmlService.ReturnsXml(StoredXml, "\"REVISION-1\"");
        _session = new(_xmlService, "definition-1");
        _js.HostModule.Returns("createDesigner", _designer);
        _designer.Returns("load", new BpmnDesignerLoadResult(true, null, [new(BpmnDesignerLoadWarningKinds.Dropped, "Lane set is not shown.", "Ls_1")]));
        _designer.Returns("save", JsonSerializer.SerializeToElement(new { xml = "<edited/>" }));
        Services.AddMudServices();
        Services.AddSingleton<IJSRuntime>(_js);
        Services.AddBpmnDesigner(new BpmnDesignerDescriptor("test-designer", "_content/Test/designer.js"));
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new DesignerOptions()));
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact(DisplayName = "The canvas loads the session's document into the enabled designer, and the session keeps its warnings")]
    public void LoadsTheSessionsDocument()
    {
        RenderCanvas();

        var create = Assert.Single(_js.HostModule.Calls);
        Assert.Equal("test-designer", create.Args[0]);
        Assert.Equal(false, create.Args[4]);
        Assert.Equal([StoredXml], Assert.Single(_designer.Calls, call => call.Identifier == "load").Args);
        Assert.True(_session.SaveGate.RequiresAcknowledgement);
        Assert.True(_session.HasCanvas);
    }

    [Fact(DisplayName = "A reload makes the canvas load the new revision; a save does not")]
    public async Task ReloadsOnlyWhatTheSessionReads()
    {
        var cut = RenderCanvas();
        _session.SaveGate.Acknowledge();
        _xmlService.AcceptsPut("\"REVISION-2\"").ReturnsXml("<reloaded/>", "\"REVISION-3\"");
        await cut.InvokeAsync(() => cut.FindComponent<BpmnCanvasHost>().Instance.OnDesignerChanged(new(["Task_1"], true, true, false)));

        await cut.InvokeAsync(() => _session.SaveAsync());
        Assert.Single(_designer.Calls, call => call.Identifier == "load");

        await cut.InvokeAsync(() => _session.ReloadAsync());
        cut.WaitForAssertion(() => Assert.Equal(2, _designer.Calls.Count(call => call.Identifier == "load")));
        Assert.Equal(["<reloaded/>"], _designer.Calls.Last(call => call.Identifier == "load").Args);
    }

    [Fact(DisplayName = "The designer's change and selection events reach the session")]
    public async Task ForwardsTheDesignersEvents()
    {
        var cut = RenderCanvas();
        var host = cut.FindComponent<BpmnCanvasHost>().Instance;

        await cut.InvokeAsync(() => host.OnDesignerChanged(new(["Task_1"], true, true, false)));
        await cut.InvokeAsync(() => host.OnDesignerSelectionChanged(new(["Task_1"], new("Task_1", "bpmn:Task", "Review"))));

        Assert.True(_session.IsDirty);
        Assert.True(_session.CanUndo);
        Assert.Equal(new BpmnDesignerSelectedElement("Task_1", "bpmn:Task", "Review"), _session.SelectedElement);
    }

    [Fact(DisplayName = "Removing the canvas disconnects it from the session")]
    public void DisposalDetaches()
    {
        var cut = RenderCanvas();

        cut.Instance.Dispose();

        Assert.False(_session.HasCanvas);
    }

    [Fact(DisplayName = "With a session, the canvas survives the editor replacing the designer, so the designer is created once")]
    public void SurvivesTheDesignerBeingReplaced()
    {
        var cut = Render<DesignerHost>(parameters => parameters.Add(x => x.Designer, NewDesigner()).Add(x => x.Session, _session));
        var canvas = cut.FindComponent<BpmnModelerCanvas>().Instance;

        cut.Render(parameters => parameters.Add(x => x.Designer, NewDesigner()).Add(x => x.Session, _session));

        Assert.Same(canvas, cut.FindComponent<BpmnModelerCanvas>().Instance);
        Assert.Single(_js.HostModule.Calls, call => call.Identifier == "createDesigner");
    }

    [Fact(DisplayName = "Without a session, each designer shows a canvas of its own, as before")]
    public void WithoutASession_EachDesignerGetsItsOwnWrapper()
    {
        var cut = Render<DesignerHost>(parameters => parameters.Add(x => x.Designer, NewDesigner()));
        var wrapper = cut.FindComponent<BpmnDesignerWrapper>().Instance;

        cut.Render(parameters => parameters.Add(x => x.Designer, NewDesigner()));

        Assert.NotSame(wrapper, cut.FindComponent<BpmnDesignerWrapper>().Instance);
        Assert.Empty(cut.FindComponents<BpmnModelerCanvas>());
    }

    /// <summary>Renders the canvas and waits for its first load, which runs after the designer is created.</summary>
    private IRenderedComponent<BpmnModelerCanvas> RenderCanvas()
    {
        var cut = Render<BpmnModelerCanvas>(parameters => parameters.Add(x => x.Session, _session));
        cut.WaitForAssertion(() => Assert.Contains(_designer.Calls, call => call.Identifier == "load"));
        return cut;
    }

    private static BpmnDiagramDesigner NewDesigner() => new(new TestLocalizer(), Microsoft.Extensions.Options.Options.Create(new DesignerOptions()), null!, null!, null!, null!);

    /// <summary>Renders a designer the way the diagram designer wrapper does, inside the cascade the editor provides.</summary>
    private sealed class DesignerHost : ComponentBase
    {
        // An empty scope, so the read-only canvas shows its notice rather than needing the X6 designer's interop.
        private static readonly JsonObject EmptyProcess = new() { ["id"] = "Process_1", ["type"] = "Elsa.BpmnProcess" };

        [Parameter] public BpmnDiagramDesigner Designer { get; set; } = null!;
        [Parameter] public BpmnModelerSession? Session { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<BpmnModelerSession?>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<BpmnModelerSession?>.Name), BpmnModelerScope.SessionCascadeName);
            builder.AddAttribute(2, nameof(CascadingValue<BpmnModelerSession?>.Value), Session);
            builder.AddAttribute(3, nameof(CascadingValue<BpmnModelerSession?>.ChildContent), Designer.DisplayDesigner(new DisplayContext(EmptyProcess)));
            builder.CloseComponent();
        }
    }
}
