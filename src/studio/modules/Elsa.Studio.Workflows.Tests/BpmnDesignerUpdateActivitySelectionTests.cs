using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDesigner.UpdateActivityAsync"/>: after an edit lands here, the component's
/// own held activity tree -- the one <c>HandleActivitySelected</c> and <c>HandleActivityDoubleClick</c>
/// resolve a selection against -- must reflect it, or a later selection would hand the properties
/// panel a pre-edit activity even though the edit was saved.
/// </summary>
public sealed class BpmnDesignerUpdateActivitySelectionTests : BunitContext, IAsyncLifetime
{
    public BpmnDesignerUpdateActivitySelectionTests()
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
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task HandleActivitySelected_ReflectsAnEarlierUpdate_ForABoundChildActivity()
    {
        var boundActivity = CreateWriteLineActivity("write-line-1", "original text");
        var root = CreateScope("root", ("ref-1", "write-line-1"), boundActivity);

        JsonObject? selected = null;
        var cut = Render<BpmnDesigner>(parameters => parameters
            .Add(p => p.Activity, root)
            .Add(p => p.ActivitySelected, EventCallback.Factory.Create<JsonObject>(this, activity => selected = activity)));

        var updatedChild = CreateWriteLineActivity("write-line-1", "updated text");
        await cut.InvokeAsync(() => cut.Instance.UpdateActivityAsync("write-line-1", updatedChild));

        var selection = new BpmnElementSelection("NotifyWarehouse", "serviceTask", "task", "Notify Warehouse", "write-line-1", "bound", "root", "root", null, null);
        await cut.InvokeAsync(() => cut.Instance.HandleActivitySelected(selection));

        Assert.NotNull(selected);
        Assert.Equal("updated text", selected!["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task HandleActivitySelected_ReflectsAnEarlierUpdate_ForTheRootScopeActivity()
    {
        var root = CreateScope("root");

        JsonObject? selected = null;
        var cut = Render<BpmnDesigner>(parameters => parameters
            .Add(p => p.Activity, root)
            .Add(p => p.ActivitySelected, EventCallback.Factory.Create<JsonObject>(this, activity => selected = activity)));

        var updatedRoot = CreateScope("root");
        updatedRoot["name"] = "updated-root";
        await cut.InvokeAsync(() => cut.Instance.UpdateActivityAsync("root", updatedRoot));

        var selection = new BpmnElementSelection("Process_1", "process", "unknown", null, null, null, "root", "root", null, null);
        await cut.InvokeAsync(() => cut.Instance.HandleActivitySelected(selection));

        Assert.NotNull(selected);
        Assert.Equal("updated-root", selected!["name"]!.GetValue<string>());
    }

    private static JsonObject CreateWriteLineActivity(string id, string text) => new()
    {
        ["id"] = id,
        ["type"] = "Elsa.WriteLine",
        ["text"] = text
    };

    /// <summary>
    /// Builds an <c>Elsa.BpmnProcess</c> scope activity carrying a single work binding and its bound
    /// child activity, mirroring <c>BpmnDesignerSelectionMappingTests</c>' fixture shape.
    /// </summary>
    private static JsonObject CreateScope(string id, (string Ref, string ActivityId) workBinding, JsonObject boundActivity) =>
        new()
        {
            ["id"] = id,
            ["type"] = "Elsa.BpmnProcess",
            ["workBindings"] = new JsonObject { [workBinding.Ref] = workBinding.ActivityId },
            ["process"] = new JsonObject { ["elements"] = new JsonArray() },
            ["activities"] = new JsonArray { boundActivity }
        };

    private static JsonObject CreateScope(string id) => new()
    {
        ["id"] = id,
        ["type"] = "Elsa.BpmnProcess",
        ["workBindings"] = new JsonObject(),
        ["process"] = new JsonObject { ["elements"] = new JsonArray() },
        ["activities"] = new JsonArray()
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
