using System.Text.Json;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDesigner"/>'s first-render path: an imported BPMN workflow's root activity,
/// supplied as the <see cref="BpmnDesigner.Activity"/> parameter, must be loaded into the graph as
/// soon as the canvas is created, without the caller having to make an explicit
/// <see cref="BpmnDesigner.LoadBpmnAsync"/> call -- otherwise the canvas opens empty until something
/// else happens to reload it.
/// </summary>
public sealed class BpmnDesignerFirstRenderLoadTests : BunitContext, IAsyncLifetime
{
    public BpmnDesignerFirstRenderLoadTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
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
    public void Render_LoadsTheSuppliedRootActivity_OnFirstRender_WithoutAnExplicitLoadCall()
    {
        var handler = JSInterop.Setup<BpmnDiagnostic[]>("loadBpmnDiagram", _ => true);
        handler.SetResult([]);

        var activity = CreateActivity("root");

        Render<BpmnDesigner>(parameters => parameters.Add(p => p.Activity, activity));

        var invocation = Assert.Single(handler.Invocations);
        var input = (JsonElement)invocation.Arguments[1]!;
        var loadedActivityId = input.GetProperty("activity").GetProperty("id").GetString();

        Assert.Equal("root", loadedActivityId);
    }

    private static JsonObject CreateActivity(string id) => new()
    {
        ["id"] = id,
        ["type"] = "Elsa.BpmnProcess",
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
