using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class SequenceMapper(IActivityMapper activityMapper) : ISequenceMapper
{
    private const string LayoutOrientationMetadataKey = "sequenceLayoutOrientation";
    private const string VerticalOrientation = "vertical";
    private const string HorizontalOrientation = "horizontal";

    public X6Graph Map(JsonObject sequence, IDictionary<string, ActivityStats>? activityStatsMap = null)
    {
        var graph = new X6Graph
        {
            LayoutOrientation = GetLayoutOrientation(sequence)
        };
        var activities = sequence.GetActivities().ToList();

        for (var index = 0; index < activities.Count; index++)
        {
            var activity = activities[index];
            var activityNodeId = activity.GetNodeId();
            var activityStats = activityStatsMap?.TryGetValue(activityNodeId, out var stats) == true ? stats : null;
            var node = activityMapper.MapActivity(activity, activityStats);

            ApplySequencePosition(node, index, graph.LayoutOrientation);
            graph.Nodes.Add(node);
        }

        foreach (var pair in activities.Zip(activities.Skip(1)))
        {
            graph.Edges.Add(new X6Edge
            {
                Shape = "elsa-sequence-edge",
                Source = new()
                {
                    Cell = pair.First.GetId(),
                    Port = "Done"
                },
                Target = new()
                {
                    Cell = pair.Second.GetId(),
                    Port = "In"
                }
            });
        }

        return graph;
    }

    public JsonObject Map(JsonObject sequence, X6Graph graph)
    {
        var orderedActivities = graph.Nodes
            .OrderBy(node => IsHorizontal(graph.LayoutOrientation) ? node.Position.X : node.Position.Y)
            .ThenBy(node => IsHorizontal(graph.LayoutOrientation) ? node.Position.Y : node.Position.X)
            .Select(node =>
            {
                var activity = node.Data;
                var designerMetadata = activity.GetDesignerMetadata();
                designerMetadata.Size = new()
                {
                    Width = node.Size.Width,
                    Height = node.Size.Height
                };
                activity.SetDesignerMetadata(designerMetadata);
                return activity;
            })
            .ToList();

        sequence.SetProperty(orderedActivities.SerializeToArray(), "activities");
        SetLayoutOrientation(sequence, graph.LayoutOrientation);

        return sequence;
    }

    public static string GetLayoutOrientation(JsonObject sequence)
    {
        if (sequence.TryGetPropertyValue("metadata", out var metadataNode) &&
            metadataNode is JsonObject metadata &&
            metadata.TryGetPropertyValue(LayoutOrientationMetadataKey, out var orientationNode))
        {
            var value = orientationNode?.GetValue<string>();
            return IsHorizontal(value) ? HorizontalOrientation : VerticalOrientation;
        }

        return VerticalOrientation;
    }

    public static void SetLayoutOrientation(JsonObject sequence, string? orientation)
    {
        var normalized = IsHorizontal(orientation) ? HorizontalOrientation : VerticalOrientation;
        var hasMetadata = sequence.TryGetPropertyValue("metadata", out var metadataNode) && metadataNode is JsonObject;
        var metadata = hasMetadata ? (JsonObject)metadataNode! : new JsonObject();

        if (normalized == VerticalOrientation)
        {
            if (hasMetadata && metadata.Remove(LayoutOrientationMetadataKey) && metadata.Count == 0)
                sequence.Remove("metadata");

            return;
        }

        metadata[LayoutOrientationMetadataKey] = normalized;
        sequence["metadata"] = metadata;
    }

    private static bool IsHorizontal(string? value) => string.Equals(value, HorizontalOrientation, StringComparison.OrdinalIgnoreCase);

    private static void ApplySequencePosition(X6ActivityNode node, int index, string? orientation)
    {
        const double gap = 160;

        if (IsHorizontal(orientation))
        {
            node.Position = new(index * gap, 0);
            return;
        }

        node.Position = new(0, index * gap);
    }
}
