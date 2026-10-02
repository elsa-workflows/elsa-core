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
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDesignerWrapper"/>'s two reasons for showing a notice instead of the X6 canvas.
/// While <see cref="DesignerOptions.UseReactFlow"/> is set, W9b's React Flow adapter for BPMN does not
/// exist yet, so the wrapper must show a clear notice rather than the X6 canvas -- and, per the issue,
/// rather than the JSON fallback or a crash. And a scope with nothing in it -- what a <c>BpmnProcess</c>
/// added to a flowchart from the toolbox is -- must say so rather than mount a canvas that would draw a
/// blank page, since importing into a nested node is not supported server-side. Otherwise it must render
/// the X6 canvas.
/// </summary>
public sealed class BpmnDesignerWrapperTests : BunitContext, IAsyncLifetime
{
    public BpmnDesignerWrapperTests()
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
    public void RendersTheReactFlowNotice_WhenUseReactFlowIsSet()
    {
        Services.Configure<DesignerOptions>(o => o.UseReactFlow = true);

        var cut = Render<BpmnDesignerWrapper>(parameters => parameters.Add(p => p.Activity, CreateActivity()));

        Assert.Contains("mud-alert", cut.Markup);
        Assert.DoesNotContain("graph-container", cut.Markup);
    }

    [Fact]
    public void RendersTheX6Canvas_WhenUseReactFlowIsNotSet()
    {
        Services.Configure<DesignerOptions>(o => o.UseReactFlow = false);

        var cut = Render<BpmnDesignerWrapper>(parameters => parameters.Add(p => p.Activity, CreateActivity()));

        Assert.Contains("graph-container", cut.Markup);
        Assert.DoesNotContain("mud-alert", cut.Markup);
    }

    /// <summary>
    /// The direction that could be mistaken for success: an empty scope on the canvas renders as a blank
    /// page that looks exactly like a diagram that failed to load, and the only trace of the reason is a
    /// console diagnostic. The notice is what tells the two apart.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void RendersTheEmptyScopeNotice_WhenTheScopeHasNoElements(int? elementCount)
    {
        Services.Configure<DesignerOptions>(o => o.UseReactFlow = false);

        var cut = Render<BpmnDesignerWrapper>(parameters => parameters.Add(p => p.Activity, CreateActivity(elementCount)));

        Assert.Contains("This BPMN scope has no content yet.", cut.Markup);
        Assert.DoesNotContain("graph-container", cut.Markup);
    }

    /// <summary>
    /// The React Flow notice keeps precedence over the empty-scope notice: on that canvas nothing is drawn
    /// either way, and naming the canvas is the more actionable of the two.
    /// </summary>
    [Fact]
    public void RendersTheReactFlowNotice_ForAnEmptyScope_WhenUseReactFlowIsSet()
    {
        Services.Configure<DesignerOptions>(o => o.UseReactFlow = true);

        var cut = Render<BpmnDesignerWrapper>(parameters => parameters.Add(p => p.Activity, CreateActivity(null)));

        Assert.Contains("BPMN is not yet available on the React Flow canvas.", cut.Markup);
        Assert.DoesNotContain("This BPMN scope has no content yet.", cut.Markup);
    }

    /// <summary>
    /// Creates a <c>BpmnProcess</c> scope carrying <paramref name="elementCount"/> elements, or no
    /// <c>process</c> payload at all when it is null -- what an empty scope created from the toolbox looks
    /// like.
    /// </summary>
    private static JsonObject CreateActivity(int? elementCount = 1)
    {
        var activity = new JsonObject
        {
            ["id"] = "root",
            ["type"] = "Elsa.BpmnProcess",
            ["version"] = 1,
            ["activities"] = new JsonArray()
        };

        if (elementCount == null)
            return activity;

        var elements = new JsonArray();

        for (var i = 0; i < elementCount; i++)
            elements.Add(new JsonObject { ["elementId"] = $"Element_{i}", ["elementType"] = "task" });

        activity["process"] = new JsonObject
        {
            ["processId"] = "process",
            ["elements"] = elements
        };

        return activity;
    }

    private class TestLocalizer : ILocalizer
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
