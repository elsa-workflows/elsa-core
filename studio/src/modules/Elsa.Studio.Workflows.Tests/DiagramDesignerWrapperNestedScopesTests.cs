using System.Reflection;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.Tests.Support;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers how <see cref="DiagramDesignerWrapper"/> treats a designer that shows the scopes nested in the one it displays
/// (<see cref="INestedScopesDiagramDesigner"/>): it reports their activities' state too, and selects one of them without
/// opening its scope. Built like <see cref="DiagramDesignerWrapperElementStatsTests"/>, with the wrapper's injected
/// services and private fields set through reflection.
/// </summary>
public class DiagramDesignerWrapperNestedScopesTests
{
    [Fact(DisplayName = "A designer that shows nested scopes gets the state of their activities too")]
    public async Task ADesignerThatShowsNestedScopes_GetsTheStateOfTheirActivities()
    {
        var executions = RecordingActivityExecutionService.Create();
        var wrapper = CreateWrapper(new RecordingDesigner(showsNestedScopes: true), executions, RecordingWorkflowDefinitionService.Create());

        await RefreshActivityStatsAsync(wrapper);

        Assert.Equal([BpmnProcessFixtures.RootId, BpmnProcessFixtures.SubprocessId], executions.Containers);
        Assert.Equal(1, GetActivityStats(wrapper)[$"{BpmnProcessFixtures.ShipActivityId}-node"].Started);
        Assert.Equal(1, GetActivityStats(wrapper)[$"{BpmnProcessFixtures.NotifyActivityId}-node"].Started);
    }

    [Fact(DisplayName = "A designer that shows one scope at a time gets the state of that scope's activities only")]
    public async Task ADesignerThatShowsOneScope_GetsTheStateOfThatScopeOnly()
    {
        var executions = RecordingActivityExecutionService.Create();
        var wrapper = CreateWrapper(new RecordingDesigner(showsNestedScopes: false), executions, RecordingWorkflowDefinitionService.Create());

        await RefreshActivityStatsAsync(wrapper);

        Assert.Equal([BpmnProcessFixtures.RootId], executions.Containers);
    }

    [Fact(DisplayName = "A nested activity is selected on the designer's own canvas, without opening its scope")]
    public async Task ANestedActivity_IsSelectedWithoutOpeningItsScope()
    {
        var designer = new RecordingDesigner(showsNestedScopes: true);
        var definitions = RecordingWorkflowDefinitionService.Create();
        var wrapper = CreateWrapper(designer, RecordingActivityExecutionService.Create(), definitions);

        await wrapper.SelectActivityByActivityIdAsync(BpmnProcessFixtures.ShipActivityId, $"{BpmnProcessFixtures.ShipActivityId}-node");

        Assert.Equal([BpmnProcessFixtures.ShipActivityId], designer.Selections);
        Assert.Equal(0, definitions.PathSegmentRequests);
    }

    [Fact(DisplayName = "For a designer that shows one scope at a time, a nested activity's scope is looked up to open it")]
    public async Task ForADesignerThatShowsOneScope_ANestedActivitysScopeIsLookedUp()
    {
        var designer = new RecordingDesigner(showsNestedScopes: false);
        var definitions = RecordingWorkflowDefinitionService.Create();
        var wrapper = CreateWrapper(designer, RecordingActivityExecutionService.Create(), definitions);

        await wrapper.SelectActivityByActivityIdAsync(BpmnProcessFixtures.ShipActivityId, $"{BpmnProcessFixtures.ShipActivityId}-node");

        Assert.Equal(1, definitions.PathSegmentRequests);
        Assert.Empty(designer.Selections);
    }

    private static Task RefreshActivityStatsAsync(DiagramDesignerWrapper wrapper) =>
        (Task)typeof(DiagramDesignerWrapper)
            .GetMethod("RefreshActivityStatsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(wrapper, null)!;

    private static IDictionary<string, ActivityStats> GetActivityStats(DiagramDesignerWrapper wrapper) =>
        (IDictionary<string, ActivityStats>)typeof(DiagramDesignerWrapper)
            .GetField("_activityStats", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(wrapper)!;

    private static DiagramDesignerWrapper CreateWrapper(
        IDiagramDesigner designer,
        RecordingActivityExecutionService executions,
        RecordingWorkflowDefinitionService definitions)
    {
        var root = BpmnProcessFixtures.OrderProcess();
        var wrapper = new DiagramDesignerWrapper
        {
            Activity = root,
            WorkflowInstanceId = "instance-1",
            WorkflowDefinitionVersionId = "definition-1"
        };

        SetProperty(wrapper, "ActivityExecutionService", executions);
        SetProperty(wrapper, "WorkflowDefinitionService", definitions);
        SetField(wrapper, "_diagramDesigner", designer);
        SetField(wrapper, "_activityGraph", BuildActivityGraph(root));

        return wrapper;
    }

    private static void SetProperty(DiagramDesignerWrapper wrapper, string name, object value) =>
        typeof(DiagramDesignerWrapper).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(wrapper, value);

    private static void SetField(DiagramDesignerWrapper wrapper, string name, object value) =>
        typeof(DiagramDesignerWrapper).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(wrapper, value);

    /// <summary>The whole process as the graph indexes it, the subprocess's body included.</summary>
    private static ActivityGraph BuildActivityGraph(JsonObject root)
    {
        var graph = new ActivityGraph(root, new FixedActivityVisitor(ToNode(root)));
        graph.IndexAsync().GetAwaiter().GetResult();
        return graph;
    }

    private static ActivityNode ToNode(JsonObject activity)
    {
        activity["nodeId"] = $"{activity["id"]}-node";
        var node = new ActivityNode(activity);

        foreach (var child in (activity["activities"] as JsonArray ?? []).Cast<JsonObject>())
        {
            var childNode = ToNode(child);
            childNode.Parents.Add(node);
            node.Children.Add(childNode);
        }

        return node;
    }

    private sealed class FixedActivityVisitor(ActivityNode root) : IActivityVisitor
    {
        public Task<ActivityNode> VisitAsync(JsonObject activity, CancellationToken cancellationToken = default) => Task.FromResult(root);
    }

    private sealed class RecordingDesigner(bool showsNestedScopes) : IDiagramDesigner, INestedScopesDiagramDesigner
    {
        public List<string> Selections { get; } = [];

        public bool ShowsNestedScopes(JsonObject container, WorkflowDefinition? workflowDefinition) => showsNestedScopes;

        public Task SelectActivityAsync(string id)
        {
            Selections.Add(id);
            return Task.CompletedTask;
        }

        public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => throw new NotSupportedException();
        public Task UpdateActivityAsync(string id, JsonObject activity) => throw new NotSupportedException();
        public Task UpdateActivityStatsAsync(string id, ActivityStats stats) => throw new NotSupportedException();
        public Task<JsonObject> ReadRootActivityAsync() => throw new NotSupportedException();
        public RenderFragment DisplayDesigner(DisplayContext context) => throw new NotSupportedException();
    }

    /// <summary>Reports each of a container's own activities as started once, as the server's report does.</summary>
    private class RecordingActivityExecutionService : DispatchProxy
    {
        public List<string> Containers { get; } = [];

        public static RecordingActivityExecutionService Create() =>
            (RecordingActivityExecutionService)(object)Create<IActivityExecutionService, RecordingActivityExecutionService>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name != nameof(IActivityExecutionService.GetReportAsync))
            {
                throw new InvalidOperationException($"Unexpected call to {targetMethod.Name}.");
            }

            var container = (JsonObject)args![1]!;
            Containers.Add(container["id"]!.GetValue<string>());

            var stats = (container["activities"] as JsonArray ?? [])
                .Cast<JsonObject>()
                .Select(activity => new ActivityExecutionStats
                {
                    ActivityId = activity["id"]!.GetValue<string>(),
                    ActivityNodeId = activity["nodeId"]!.GetValue<string>(),
                    StartedCount = 1
                })
                .ToList();

            return Task.FromResult(new ActivityExecutionReport(stats));
        }
    }

    /// <summary>Counts the requests for a node's path, and finds none.</summary>
    private class RecordingWorkflowDefinitionService : DispatchProxy
    {
        public int PathSegmentRequests { get; private set; }

        public static RecordingWorkflowDefinitionService Create() =>
            (RecordingWorkflowDefinitionService)(object)Create<IWorkflowDefinitionService, RecordingWorkflowDefinitionService>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name != nameof(IWorkflowDefinitionService.GetPathSegmentsAsync))
            {
                throw new InvalidOperationException($"Unexpected call to {targetMethod.Name}.");
            }

            PathSegmentRequests++;
            return Task.FromResult<GetPathSegmentsResponse?>(null);
        }
    }
}
