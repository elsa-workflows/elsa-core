using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Extensions;

/// A set of extensions for <see cref="IActivityVisitor"/>.
public static class ActivityVisitorExtensions
{
    /// Returns a lookup dictionary by activity ID.
    public static async Task<IDictionary<string, ActivityNode>> VisitAndMapAsync(this IActivityVisitor visitor, WorkflowDefinition workflowDefinition)
    {
        var workflowActivity = ToActivity(workflowDefinition);
        return await visitor.VisitAndMapAsync(workflowActivity);
    }
    
    /// Returns a lookup dictionary by activity ID.
    public static async Task<ActivityNode> VisitAsync(this IActivityVisitor visitor, WorkflowDefinition workflowDefinition)
    {
        var workflowActivity = ToActivity(workflowDefinition);
        return await visitor.VisitAsync(workflowActivity);
    }
    
    /// Returns a lookup dictionary by activity ID.
    public static async Task<IDictionary<string, ActivityNode>> VisitAndMapAsync(this IActivityVisitor visitor, JsonObject activity)
    {
        var graph = await visitor.VisitAsync(activity);
        var nodes = graph.Flatten();
        return nodes.GroupBy(n => n.NodeId).Select(g => g.First()).ToDictionary(x => x.NodeId);
    }
    
    /// Creates an activity graph based on the provided workflow definition.
    public static async Task<ActivityGraph> VisitAndCreateGraphAsync(this IActivityVisitor visitor, WorkflowDefinition workflowDefinition)
    {
        var workflowActivity = ToActivity(workflowDefinition);
        return await visitor.VisitAndCreateGraphAsync(workflowActivity);
    }
    
    /// Creates an activity graph based on the provided JSON activity.
    public static async Task<ActivityGraph> VisitAndCreateGraphAsync(this IActivityVisitor visitor, JsonObject activity)
    {
        return await new ActivityGraph(activity, visitor).IndexAsync();
    }
    
    /// Creates an activity from the specified workflow definition.
    private static JsonObject ToActivity(WorkflowDefinition workflowDefinition)
    {
        var workflowActivity = new JsonObject();
        workflowActivity.SetId("Workflow1");
        workflowActivity.SetNodeId("Workflow1");
        workflowActivity.SetTypeName("Elsa.Workflow");
        workflowActivity.SetVersion(1);
        workflowActivity.SetName(workflowDefinition.Name);
        workflowActivity.SetRoot(workflowDefinition.Root);
        return workflowActivity;
    }

}