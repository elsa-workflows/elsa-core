using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class FlowchartMapper(IActivityMapper activityMapper) : IFlowchartMapper
{
    /// <summary>
    /// Provides the map.
    /// </summary>
    public X6Graph Map(JsonObject flowchart, IDictionary<string, ActivityStats>? activityStatsMap = null)
    {
        var graph = new X6Graph();
        var activities = flowchart.GetActivities();
        var connections = flowchart.GetConnections();

        foreach (var activity in activities)
        {
            var activityNodeId = activity.GetNodeId();
            var activityStats = activityStatsMap?.TryGetValue(activityNodeId, out var stats) == true ? stats : null;
            var node = activityMapper.MapActivity(activity, activityStats);
            graph.Nodes.Add(node);
        }

        foreach (var connection in connections)
        {
            var source = connection.Source;
            var target = connection.Target;
            var sourceId = source.ActivityId;
            var sourcePort = source.Port ?? "Done";
            var targetId = target.ActivityId;
            var targetPort = target.Port ?? "In";
            var connector = new X6Edge
            {
                Shape = "elsa-edge",
                Source = new()
                {
                    Cell = sourceId,
                    Port = sourcePort
                },
                Target = new()
                {
                    Cell = targetId,
                    Port = targetPort
                },
                Vertices = connection.Vertices.Select(x => new X6Position(x.X, x.Y)).ToList()
            };

            graph.Edges.Add(connector);
        }

        return graph;
    }

    /// <summary>
    /// Performs the map operation.
    /// </summary>
    /// <param name="graph">The graph.</param>
    /// <returns>The result of the operation.</returns>
    public JsonObject Map(X6Graph graph)
    {
        var activities = new List<JsonObject>();
        var connections = new List<Connection>();

        foreach (var node in graph.Nodes)
        {
            var activity = node.Data;
            var designerMetadata = activity.GetDesignerMetadata();

            designerMetadata.Position = new()
            {
                X = node.Position.X,
                Y = node.Position.Y
            };

            designerMetadata.Size = new()
            {
                Width = node.Size.Width,
                Height = node.Size.Height
            };

            activity.SetDesignerMetadata(designerMetadata);
            activities.Add(activity);
        }

        foreach (var edge in graph.Edges)
        {
            var connection = new Connection
            {
                Source = new(edge.Source.Cell, edge.Source.Port),
                Target = new(edge.Target.Cell, edge.Target.Port),
                Vertices = edge.Vertices.Select(x => new Position(x.X, x.Y)).ToList(),
            };
            connections.Add(connection);
        }

        var flowchart = new JsonObject(new Dictionary<string, JsonNode?>
        {
            ["activities"] = activities.SerializeToArray(),
            ["connections"] = connections.SerializeToArray()
        });

        return flowchart;
    }
}