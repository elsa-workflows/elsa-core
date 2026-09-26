using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Models;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public class SequenceMapperTests
{
    private readonly SequenceMapper _mapper = new(new StubActivityMapper());

    [Fact]
    public void Map_ShouldCreateOrderedNodesAndDerivedEdges()
    {
        var sequence = CreateSequence("one", "two", "three");

        var graph = _mapper.Map(sequence);

        Assert.Equal("vertical", graph.LayoutOrientation);
        Assert.Equal(["one", "two", "three"], graph.Nodes.Select(x => x.Id));
        Assert.Equal([0, 160, 320], graph.Nodes.Select(x => x.Position.Y));
        Assert.Equal(2, graph.Edges.Count);
        Assert.Collection(
            graph.Edges,
            edge => AssertEdge(edge, "one", "two"),
            edge => AssertEdge(edge, "two", "three"));
    }

    [Fact]
    public void Map_ShouldReadBackActivitiesInVisualOrderAndDiscardEdges()
    {
        var sequence = CreateSequence("one", "two", "three");
        var graph = new X6Graph(
            [
                CreateNode("three", 0, 320),
                CreateNode("one", 0, 0),
                CreateNode("two", 0, 160)
            ],
            [
                new X6Edge
                {
                    Shape = "elsa-edge",
                    Source = new() { Cell = "three", Port = "Done" },
                    Target = new() { Cell = "one", Port = "In" }
                }
            ]);

        var mapped = _mapper.Map(sequence, graph);

        Assert.Equal(["one", "two", "three"], mapped.GetActivities().Select(x => x.GetId()));
        Assert.False(mapped.ContainsKey("connections"));
    }

    [Fact]
    public void Map_ShouldOrderByHorizontalPositionAndPersistOrientation()
    {
        var sequence = CreateSequence("one", "two", "three");
        var graph = new X6Graph(
            [
                CreateNode("two", 160, 0),
                CreateNode("three", 320, 0),
                CreateNode("one", 0, 0)
            ],
            [])
        {
            LayoutOrientation = "horizontal"
        };

        var mapped = _mapper.Map(sequence, graph);
        var remapped = _mapper.Map(mapped);

        Assert.Equal(["one", "two", "three"], mapped.GetActivities().Select(x => x.GetId()));
        Assert.Equal("horizontal", SequenceMapper.GetLayoutOrientation(mapped));
        Assert.Equal([0, 160, 320], remapped.Nodes.Select(x => x.Position.X));
        Assert.All(remapped.Nodes, node => Assert.Equal(0, node.Position.Y));
    }

    [Fact]
    public void GetLayoutOrientation_ShouldDefaultToVertical()
    {
        var sequence = CreateSequence("one");

        Assert.Equal("vertical", SequenceMapper.GetLayoutOrientation(sequence));
    }

    [Fact]
    public void Map_ShouldNotWriteDefaultOrientationMetadata()
    {
        var sequence = CreateSequence("one");
        var graph = new X6Graph([CreateNode("one", 0, 0)], [])
        {
            LayoutOrientation = "vertical"
        };

        var mapped = _mapper.Map(sequence, graph);

        Assert.False(mapped.ContainsKey("metadata"));
    }

    [Fact]
    public void Map_ShouldClearOrientationMetadataWhenReturningToDefault()
    {
        var sequence = CreateSequence("one");
        sequence["metadata"] = new JsonObject
        {
            ["sequenceLayoutOrientation"] = "horizontal"
        };
        var graph = new X6Graph([CreateNode("one", 0, 0)], [])
        {
            LayoutOrientation = "vertical"
        };

        var mapped = _mapper.Map(sequence, graph);

        Assert.False(mapped.ContainsKey("metadata"));
    }

    [Fact]
    public void Map_ShouldPreserveActivityConfigurationAndEmbeddedRegions()
    {
        var sequence = CreateSequence("one", "two");
        var configuredActivity = CreateActivity("configured");
        configuredActivity["input"] = "value";
        configuredActivity["body"] = new JsonObject
        {
            ["id"] = "embedded-sequence",
            ["type"] = "Elsa.Sequence",
            ["activities"] = new JsonArray(CreateActivity("nested"))
        };
        var graph = new X6Graph([CreateNode("one", 0, 0), CreateNode(configuredActivity, 0, 160), CreateNode("two", 0, 320)], []);

        var mapped = _mapper.Map(sequence, graph);
        var activities = mapped.GetActivities().ToList();

        Assert.Equal(["one", "configured", "two"], activities.Select(x => x.GetId()));
        Assert.Equal("value", activities[1]["input"]!.GetValue<string>());
        Assert.Equal("embedded-sequence", activities[1]["body"]!["id"]!.GetValue<string>());
        Assert.Equal("nested", activities[1]["body"]!["activities"]![0]!["id"]!.GetValue<string>());
    }

    private static JsonObject CreateSequence(params string[] activityIds) =>
        new()
        {
            ["id"] = "sequence",
            ["type"] = "Elsa.Sequence",
            ["activities"] = new JsonArray(activityIds.Select(CreateActivity).ToArray<JsonNode?>())
        };

    private static JsonObject CreateActivity(string id) =>
        new()
        {
            ["id"] = id,
            ["type"] = "Test.Activity"
        };

    private static X6ActivityNode CreateNode(string id, double x, double y) =>
        CreateNode(CreateActivity(id), x, y);

    private static X6ActivityNode CreateNode(JsonObject activity, double x, double y) =>
        new()
        {
            Id = activity.GetId(),
            Shape = "activity",
            Data = activity,
            Position = new X6Position(x, y),
            Size = new X6Size(240, 120),
            Ports = new X6Ports()
        };

    private static void AssertEdge(X6Edge edge, string sourceId, string targetId)
    {
        Assert.Equal("elsa-sequence-edge", edge.Shape);
        Assert.Equal(sourceId, edge.Source.Cell);
        Assert.Equal("Done", edge.Source.Port);
        Assert.Equal(targetId, edge.Target.Cell);
        Assert.Equal("In", edge.Target.Port);
    }

    private sealed class StubActivityMapper : IActivityMapper
    {
        public X6ActivityNode MapActivity(JsonObject activity, ActivityStats? activityStats = null) =>
            new()
            {
                Id = activity.GetId(),
                Shape = "activity",
                Data = activity,
                Position = new X6Position(100, 100),
                Size = new X6Size(240, 120),
                Ports = new X6Ports(),
                ActivityStats = activityStats
            };

        public IEnumerable<X6Port> GetOutPorts(JsonObject activity) => [];

        public IEnumerable<X6Port> GetInPorts(JsonObject activity) => [];

        public X6Ports GetPorts(JsonObject activity) => new();
    }
}
