using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the element-stats mount race (issue's W13 follow-up): a refresh can name a
/// <see cref="BpmnDiagramDesigner"/> before its <see cref="BpmnDesignerWrapper"/> -- and, in turn, the canvas
/// underneath it -- has ever been rendered, most notably the one unconditional refresh a freshly opened instance
/// gets on load, which runs before the designer has had a chance to render anything at all. That update must not
/// be silently dropped: it is the only chance a finished instance (no observer-driven refresh to fall back on)
/// ever gets to show its gateway, event and taken-flow overlay.
/// </summary>
public sealed class BpmnDiagramDesignerElementStatsMountRaceTests : BunitContext, IAsyncLifetime
{
    public BpmnDiagramDesignerElementStatsMountRaceTests()
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
    public async Task UpdateElementStatsAsync_BeforeTheCanvasIsMounted_IsAppliedOnceItMounts()
    {
        var handler = JSInterop.SetupVoid("updateBpmnElementStats", _ => true);

        var designer = new BpmnDiagramDesigner(new TestLocalizer(), Microsoft.Extensions.Options.Options.Create(new DesignerOptions()), null!, null!, null!, null!);
        var activity = CreateActivity("root");
        var elementStats = new Dictionary<string, BpmnElementStats>
        {
            ["Element_1"] = new() { Completed = 1 }
        };

        // Simulate the race: the refresh arrives before the designer's wrapper -- and its canvas -- has ever been
        // rendered, exactly as it does today during DiagramDesignerWrapper.OnInitializedAsync's unconditional
        // first refresh, which runs before the render tree that would mount BpmnDesignerWrapper exists yet.
        await designer.UpdateElementStatsAsync(elementStats);

        Assert.Empty(handler.Invocations);

        // Mounting the designer's render fragment is what today's production code does next, once the render
        // tree containing it is actually processed by the renderer.
        var context = new DisplayContext(activity);
        Render(designer.DisplayDesigner(context));

        var invocation = Assert.Single(handler.Invocations);
        var appliedStats = Assert.IsType<Dictionary<string, BpmnElementStats>>(invocation.Arguments[1]);
        Assert.Equal(1, appliedStats["Element_1"].Completed);
    }

    /// <summary>
    /// Creates a <c>BpmnProcess</c> root carrying one element, so <see cref="BpmnDesignerWrapper"/> mounts the
    /// canvas rather than the "empty scope" notice.
    /// </summary>
    private static JsonObject CreateActivity(string id) => new()
    {
        ["id"] = id,
        ["type"] = "Elsa.BpmnProcess",
        ["version"] = 1,
        ["activities"] = new JsonArray(),
        ["process"] = new JsonObject
        {
            ["processId"] = "process",
            ["elements"] = new JsonArray
            {
                new JsonObject { ["elementId"] = "Element_1", ["elementType"] = "task" }
            }
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
