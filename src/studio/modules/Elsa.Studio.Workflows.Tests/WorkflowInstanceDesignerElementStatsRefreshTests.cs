using System.Reflection;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers that <see cref="WorkflowInstanceDesigner"/> refreshes the element-keyed BPMN overlay
/// (<see cref="DiagramDesignerWrapper.RefreshElementStatsAsync"/>) on the very same event that already refreshes
/// the activity-keyed one -- <see cref="IWorkflowInstanceObserver.ActivityExecutionLogUpdated"/> -- so a gateway or
/// a sequence flow, which never appears in that message's own <c>Stats</c>, is refreshed on the same cadence.
/// </summary>
/// <remarks>
/// The refresh added here rides the very same event subscription <c>WorkflowInstanceDesignerDisconnectRefreshTests</c>
/// already proves is detached on disposal (the #992 guarantee): it is not a new timer with a disposal race of its
/// own to pin, so this file's own coverage is limited to proving the new call is actually wired into that handler.
/// </remarks>
public sealed class WorkflowInstanceDesignerElementStatsRefreshTests : BunitContext, IAsyncLifetime
{
    public WorkflowInstanceDesignerElementStatsRefreshTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ILocalizer>(new TestLocalizer());
        Services.AddSingleton<IActivityRegistry>(new ActivityRegistryStub());
        Services.AddSingleton(DispatchProxy.Create<IDiagramDesignerService, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IDomAccessor, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IActivityVisitor, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceObserverFactory, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceService, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowDefinitionService, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IActivityExecutionService, ThrowingProxy>());
        Services.AddSingleton<IRemoteFeatureProvider>(new RemoteFeatureProviderStub());
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task OnActivityExecutionLogUpdated_RefreshesTheElementKeyedOverlay()
    {
        var cut = RenderDesigner();
        var designer = new RecordingDiagramDesignerWrapper();
        SetDesigner(cut.Instance, designer);

        await InvokeOnActivityExecutionLogUpdated(cut.Instance, new ActivityExecutionLogUpdatedMessage([]));

        Assert.Equal(1, designer.RefreshElementStatsCallCount);
    }

    [Fact]
    public async Task OnActivityExecutionLogUpdated_RefreshesTheElementKeyedOverlay_AlongsideActivityStats()
    {
        // Both the pre-existing activity-keyed stats and the new element-keyed overlay are refreshed from the
        // one message: the point of D1 is that a gateway or a flow, which never appears in message.Stats, is
        // still covered by something driven from the very same journal update.
        var stats = new ActivityExecutionStats { ActivityId = "activity-1", ActivityNodeId = "node-1" };
        var cut = RenderDesigner();
        var designer = new RecordingDiagramDesignerWrapper();
        var innerDesigner = new RecordingActivityStatsDesigner();
        SetDesigner(cut.Instance, designer);
        SetInnerDiagramDesigner(designer, innerDesigner);

        await InvokeOnActivityExecutionLogUpdated(cut.Instance, new ActivityExecutionLogUpdatedMessage([stats]));

        Assert.Equal(1, innerDesigner.UpdateActivityStatsCallCount);
        Assert.Equal(1, designer.RefreshElementStatsCallCount);
    }

    private IRenderedComponent<TestWorkflowInstanceDesigner> RenderDesigner()
    {
        var workflowInstance = new WorkflowInstance
        {
            Id = "instance-1",
            DefinitionId = "definition-1",
            Status = WorkflowStatus.Finished
        };

        return Render<TestWorkflowInstanceDesigner>(parameters => parameters
            .Add(x => x.WorkflowInstance, workflowInstance));
    }

    /// <summary>
    /// A <see cref="WorkflowInstanceDesigner"/> that skips its own markup and first-render setup, exactly like
    /// <c>WorkflowInstanceDesignerDisconnectRefreshTests.TestWorkflowInstanceDesigner</c>: nothing under test here
    /// depends on the real render tree, and rendering it for real would pull in dependencies (Radzen's splitter,
    /// MudBlazor's tabs) this test has no reason to stub.
    /// </summary>
    private sealed class TestWorkflowInstanceDesigner : WorkflowInstanceDesigner
    {
        protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private static Task InvokeOnActivityExecutionLogUpdated(WorkflowInstanceDesigner instance, ActivityExecutionLogUpdatedMessage message)
    {
        var method = typeof(WorkflowInstanceDesigner).GetMethod("OnActivityExecutionLogUpdated", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(instance, [message])!;
    }

    /// <summary>
    /// Attaches a bare <see cref="DiagramDesignerWrapper"/> subclass to <c>_designer</c>, mirroring
    /// <c>WorkflowInstanceDesignerDisconnectRefreshTests.SetDesigner</c>.
    /// </summary>
    private static void SetDesigner(WorkflowInstanceDesigner instance, DiagramDesignerWrapper designer)
    {
        var field = typeof(WorkflowInstanceDesigner).GetField("_designer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, designer);
    }

    /// <summary>Attaches a fake <see cref="IDiagramDesigner"/> to a wrapper's own private <c>_diagramDesigner</c> field.</summary>
    private static void SetInnerDiagramDesigner(DiagramDesignerWrapper wrapper, IDiagramDesigner innerDesigner)
    {
        var field = typeof(DiagramDesignerWrapper).GetField("_diagramDesigner", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(wrapper, innerDesigner);
    }

    /// <summary>A <see cref="DiagramDesignerWrapper"/> whose element-stats refresh is counted instead of run for real.</summary>
    private sealed class RecordingDiagramDesignerWrapper : DiagramDesignerWrapper
    {
        public int RefreshElementStatsCallCount { get; private set; }

        internal override Task RefreshElementStatsAsync()
        {
            RefreshElementStatsCallCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>The fake <see cref="IDiagramDesigner"/> a <see cref="DiagramDesignerWrapper"/> forwards to; records
    /// activity-stats updates so the test can prove both overlays are refreshed from the one message.</summary>
    private sealed class RecordingActivityStatsDesigner : IDiagramDesigner
    {
        public int UpdateActivityStatsCallCount { get; private set; }

        public Task UpdateActivityStatsAsync(string id, ActivityStats stats)
        {
            UpdateActivityStatsCallCount++;
            return Task.CompletedTask;
        }

        public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => throw new NotSupportedException();
        public Task UpdateActivityAsync(string id, JsonObject activity) => throw new NotSupportedException();
        public Task SelectActivityAsync(string id) => throw new NotSupportedException();
        public Task<JsonObject> ReadRootActivityAsync() => throw new NotSupportedException();
        public RenderFragment DisplayDesigner(DisplayContext context) => throw new NotSupportedException();
    }

    private sealed class RemoteFeatureProviderStub : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ActivityRegistryStub : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor> List() => throw new NotSupportedException();
        public Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor? Find(string activityType, int? version = null) => throw new NotSupportedException();
        public IEnumerable<Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor> FindAll(string activityType) => throw new NotSupportedException();
        public void MarkStale() => throw new NotSupportedException();
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    /// <summary>A <see cref="DispatchProxy"/> that throws for every call, for services this test never exercises.</summary>
    private class ThrowingProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected call to {targetMethod!.DeclaringType!.Name}.{targetMethod.Name}.");
    }
}
