using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers how the "Performed by" section learns which BPMN element was clicked. <c>ActivitySelected</c> can only name an
/// activity, so a click on a task nothing is bound to yet looks exactly like a click on the empty canvas; the element
/// itself has to reach the editor separately, including when no activity is bound to it, and a click on the canvas has
/// to clear it.
/// </summary>
public sealed class BpmnElementSelectionTests : BunitContext, IAsyncLifetime
{
    private const string ProcessId = "order-process";
    private static readonly BpmnElementSelection UnboundTask = new("NotifyWarehouse", "serviceTask", "task", "Notify Warehouse", null, "unbound", ProcessId, ProcessId, null, null, BpmnBindingKinds.UnboundTask);

    private readonly List<BpmnElementSelection?> _elements = [];
    private readonly List<JsonObject> _activities = [];

    public BpmnElementSelectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<BpmnDiagnostic[]>("loadBpmnDiagram", _ => true).SetResult([]);
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddWorkflowsCore();
        Services.AddWorkflowsDesigner();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry, NoOpActivityRegistry>();
        Services.Configure<DesignerOptions>(options => options.UseReactFlow = false);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task SelectingATaskNothingIsBoundTo_RaisesTheElement_WhileActivitySelectedCanOnlyNameTheScope()
    {
        var cut = Render<BpmnDesigner>(parameters => parameters
            .Add(p => p.Activity, RootActivity())
            .Add(p => p.ActivitySelected, EventCallback.Factory.Create<JsonObject>(this, _activities.Add))
            .Add(p => p.ElementSelected, EventCallback.Factory.Create<BpmnElementSelection?>(this, _elements.Add)));

        await cut.InvokeAsync(() => cut.Instance.HandleActivitySelected(UnboundTask));

        Assert.Same(UnboundTask, Assert.Single(_elements));
        Assert.Equal(ProcessId, Assert.Single(_activities)["id"]!.GetValue<string>());
    }

    [Fact]
    public async Task SelectingTheCanvas_ClearsTheElement()
    {
        var cut = Render<BpmnDesigner>(parameters => parameters
            .Add(p => p.Activity, RootActivity())
            .Add(p => p.ElementSelected, EventCallback.Factory.Create<BpmnElementSelection?>(this, _elements.Add)));

        await cut.InvokeAsync(() => cut.Instance.HandleCanvasSelected());

        Assert.Null(Assert.Single(_elements));
    }

    [Fact]
    public async Task TheWrapper_HandsTheCanvasTheReceiverAnEditorCascades()
    {
        var cut = Render<BpmnDesignerWrapper>(parameters => parameters
            .Add(p => p.Activity, RootActivity())
            .AddCascadingValue(BpmnDesignerWrapper.ElementSelectedCascadeName, EventCallback.Factory.Create<BpmnElementSelection?>(this, _elements.Add)));

        var canvas = cut.FindComponent<BpmnDesigner>();
        await cut.InvokeAsync(() => canvas.Instance.HandleActivitySelected(UnboundTask));
        await cut.InvokeAsync(() => canvas.Instance.HandleCanvasSelected());

        Assert.Equal([UnboundTask, null], _elements);
    }

    [Fact]
    public void TheWrapper_RaisesNothing_WhereNoEditorCascadesAReceiver()
    {
        // The read-only viewers host the same canvas without a "Performed by" section to feed.
        var cut = Render<BpmnDesignerWrapper>(parameters => parameters.Add(p => p.Activity, RootActivity()));

        Assert.False(cut.FindComponent<BpmnDesigner>().Instance.ElementSelected.HasDelegate);
    }

    /// <summary>A scope whose one task nothing is bound to yet.</summary>
    private static JsonObject RootActivity() => new()
    {
        ["id"] = ProcessId,
        ["type"] = "Elsa.BpmnProcess",
        ["version"] = 1,
        ["workBindings"] = new JsonObject(),
        ["activities"] = new JsonArray(),
        ["process"] = new JsonObject
        {
            ["processId"] = ProcessId,
            ["elements"] = new JsonArray(new JsonObject { ["elementId"] = "NotifyWarehouse", ["elementType"] = "serviceTask" })
        }
    };

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class NoOpActivityRegistry : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => [];
        public ActivityDescriptor? Find(string activityType, int? version = default) => null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => [];

        public void MarkStale()
        {
        }
    }
}
