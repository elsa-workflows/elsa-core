using System.Text.Json;
using Bunit;
using Elsa.Studio.Bpmn.Components;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Bpmn.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Elsa.Studio.Bpmn.Tests;

public class BpmnCanvasHostTests : IAsyncDisposable
{
    private static readonly BpmnDesignerDescriptor Designer = new("bpmn-js", "_content/Elsa.Studio.Bpmn.BpmnJs/bpmn-js-designer.js", "_content/Elsa.Studio.Bpmn.BpmnJs/bpmn-js-designer.css");

    private readonly BunitContext _context = new();
    private readonly FakeJsRuntime _js = new();
    private readonly FakeJsObject _designer = new();

    public BpmnCanvasHostTests()
    {
        _context.Services.AddSingleton<IJSRuntime>(_js);
        _js.HostModule.Returns("createDesigner", _designer);
    }

    [Fact(DisplayName = "The host imports its module and creates the chosen designer in its canvas, read-only when asked")]
    public void CreatesTheChosenDesigner()
    {
        Render(readOnly: true);

        var import = Assert.Single(_js.Calls);
        Assert.Equal("import", import.Identifier);
        Assert.Equal(["./_content/Elsa.Studio.Bpmn/bpmn-canvas-host.js"], import.Args);

        var create = Assert.Single(_js.HostModule.Calls);
        Assert.Equal("createDesigner", create.Identifier);
        Assert.Equal(["bpmn-js", Designer.ModuleUrl, Designer.StylesheetUrl], create.Args.Take(3));
        Assert.IsType<ElementReference>(create.Args[3]);
        Assert.Equal(true, create.Args[4]);
        Assert.IsType<DotNetObjectReference<BpmnCanvasHost>>(create.Args[5]);
    }

    [Fact(DisplayName = "Each method calls the designer's method of the same name and returns its answer")]
    public async Task MethodsCallTheDesigner()
    {
        var loaded = new BpmnDesignerLoadResult(true, null, [new(BpmnDesignerLoadWarningKinds.Rewritten, "Layout moved.", "Sub_1")]);
        var binding = new BpmnDesignerBinding("Elsa.WriteLine", [new("text", "\"hello\"")]);
        var overlays = new BpmnDesignerOverlayResult(["Task_1"], ["Gone"]);
        _designer.Returns("load", loaded);
        _designer.Returns("save", JsonSerializer.SerializeToElement(new { xml = "<saved/>" }));
        _designer.Returns("getBinding", binding);
        _designer.Returns("setBinding", new BpmnDesignerSetBindingResult(false, BpmnDesignerSetBindingRefusals.NotBindable));
        _designer.Returns("applyOverlays", overlays);
        var host = Render().Instance;
        var stats = new Dictionary<string, BpmnDesignerElementStats> { ["Task_1"] = new() { Active = 1 } };

        Assert.Same(loaded, await host.LoadAsync("<definitions/>"));
        Assert.Equal("<saved/>", await host.SaveAsync());
        Assert.Same(binding, await host.GetBindingAsync("Task_1"));
        Assert.Equal(new BpmnDesignerSetBindingResult(false, "not-bindable"), await host.SetBindingAsync("Gateway_1", binding));
        Assert.Same(overlays, await host.ApplyOverlaysAsync(stats));

        Assert.Equal(["load", "save", "getBinding", "setBinding", "applyOverlays"], _designer.Calls.Select(call => call.Identifier));
        Assert.Equal(["<definitions/>"], _designer.Calls[0].Args);
        Assert.Equal(["Gateway_1", binding], _designer.Calls[3].Args);
        Assert.Same(stats, _designer.Calls[4].Args[0]);
    }

    [Fact(DisplayName = "A call made while the designer is still being created waits for it")]
    public async Task CallsWaitForTheDesigner()
    {
        var created = new TaskCompletionSource<object?>();
        _js.HostModule.Handlers["createDesigner"] = _ => created.Task;
        _designer.Returns("select", true);
        var host = Render().Instance;

        var select = host.SelectAsync("Task_1");

        Assert.False(select.IsCompleted);
        Assert.Empty(_designer.Calls);

        created.SetResult(_designer);

        Assert.True(await select);
        Assert.Equal(["Task_1"], Assert.Single(_designer.Calls).Args);
    }

    [Fact(DisplayName = "A designer that cannot be created is reported in the canvas, and calls to it fail")]
    public async Task ACreationFailure_IsShownAndCallsFail()
    {
        _js.HostModule.Handlers["createDesigner"] = _ => throw new JSException("The module implements contract version 2, but this host supports version 1.");
        var cut = Render();

        var alert = cut.Find("[role=alert]");
        Assert.Contains("'bpmn-js'", alert.TextContent);
        Assert.Contains("contract version 2", alert.TextContent);
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Instance.LoadAsync("<definitions/>"));
    }

    [Fact(DisplayName = "The designer's change and selection events reach the component's callbacks")]
    public async Task EventsReachTheCallbacks()
    {
        BpmnDesignerChange? change = null;
        BpmnDesignerSelection? selection = null;
        var cut = _context.Render<BpmnCanvasHost>(parameters => parameters
            .Add(host => host.Designer, Designer)
            .Add(host => host.Changed, value => change = value)
            .Add(host => host.SelectionChanged, value => selection = value));
        var raisedChange = new BpmnDesignerChange(["Task_1"], true, true, false);
        var raisedSelection = new BpmnDesignerSelection(["Task_1"], new("Task_1", "bpmn:Task", "Ship"));

        await cut.InvokeAsync(() => cut.Instance.OnDesignerChanged(raisedChange));
        await cut.InvokeAsync(() => cut.Instance.OnDesignerSelectionChanged(raisedSelection));

        Assert.Same(raisedChange, change);
        Assert.Same(raisedSelection, selection);
    }

    [Fact(DisplayName = "Disposing the component disposes the designer and releases its references")]
    public async Task Dispose_DisposesTheDesigner()
    {
        var host = Render().Instance;

        await host.DisposeAsync();

        Assert.Equal("dispose", Assert.Single(_designer.Calls).Identifier);
        Assert.True(_designer.IsDisposed);
        Assert.True(_js.HostModule.IsDisposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => host.SaveAsync());
    }

    [Fact(DisplayName = "Disposing the component while the designer is being created releases the designer once it exists")]
    public async Task DisposeDuringCreation_ReleasesTheDesigner()
    {
        var created = new TaskCompletionSource<object?>();
        _js.HostModule.Handlers["createDesigner"] = _ => created.Task;
        var cut = Render();

        await cut.Instance.DisposeAsync();
        created.SetResult(_designer);

        cut.WaitForAssertion(() => Assert.True(_designer.IsDisposed));
        Assert.Equal("dispose", Assert.Single(_designer.Calls).Identifier);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private IRenderedComponent<BpmnCanvasHost> Render(bool readOnly = false) =>
        _context.Render<BpmnCanvasHost>(parameters => parameters
            .Add(host => host.Designer, Designer)
            .Add(host => host.ReadOnly, readOnly));
}
