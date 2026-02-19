using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowReferenceGraphBuilderTests
{
    private readonly IWorkflowReferenceQuery _query = Substitute.For<IWorkflowReferenceQuery>();

    [Fact]
    public async Task BuildGraphAsync_WithNoConsumers_ReturnsEmptyGraph()
    {
        _query
            .ExecuteAsync("A", Arg.Any<CancellationToken>())
            .Returns([]);

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        Assert.Single(graph.RootDefinitionIds);
        Assert.Contains("A", graph.RootDefinitionIds);
        Assert.Empty(graph.Edges);
        Assert.Empty(graph.ConsumerDefinitionIds);
        Assert.Single(graph.AllDefinitionIds);
    }

    [Fact]
    public async Task BuildGraphAsync_WithDirectConsumers_ReturnsEdgesAndConsumers()
    {
        SetupGraph(
            ("A", ["B", "C"]),
            ("B", []),
            ("C", [])
        );

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        // Edges.
        AssertEdges(graph, ("B", "A"), ("C", "A"));

        // Consumer IDs.
        AssertConsumers(graph, "B", "C");

        // Inbound lookup on target "A" returns the same consumers.
        var consumers = graph.GetConsumers("A").ToList();
        Assert.Equal(2, consumers.Count);
        Assert.Contains("B", consumers);
        Assert.Contains("C", consumers);
    }

    [Fact]
    public async Task BuildGraphAsync_WithTransitiveConsumers_ReturnsFullGraph()
    {
        SetupGraph(
            ("A", ["B"]),
            ("B", ["C"]),
            ("C", [])
        );

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        AssertEdges(graph, ("B", "A"), ("C", "B"));
        AssertConsumers(graph, "B", "C");
    }

    [Fact]
    public async Task BuildGraphAsync_WithDiamondGraph_IncludesAllEdges()
    {
        SetupGraph(
            ("A", ["B", "C"]),
            ("B", ["D"]),
            ("C", ["D"]),
            ("D", [])
        );

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        // All three consumers should be discovered.
        AssertConsumers(graph, "B", "C", "D");

        // Edges: B→A, D→B (from B's branch), C→A, D→C (edge yielded before recursion is skipped).
        AssertEdges(graph, ("B", "A"), ("D", "B"), ("C", "A"), ("D", "C"));
    }

    [Fact]
    public async Task BuildGraphAsync_WithDiamondGraph_DoesNotRecurseAlreadyVisitedNodes()
    {
        SetupGraph(
            ("A", ["B", "C"]),
            ("B", ["D"]),
            ("C", ["D"]),
            ("D", ["E"]),
            ("E", [])
        );

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        // D's recursive processing (E→D) happens only from B's branch.
        // From C's branch, D→C edge is yielded but D is already visited, so E is not re-explored.
        Assert.Contains(graph.Edges, e => e is { Source: "E", Target: "D" });
        Assert.Equal(1, graph.Edges.Count(e => e is { Source: "E", Target: "D" }));
    }

    [Fact]
    public async Task BuildGraphAsync_WithCycle_DoesNotInfiniteLoop()
    {
        SetupGraph(
            ("A", ["B"]),
            ("B", ["C"]),
            ("C", ["A"])
        );

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        // Edges: B→A, C→B, A→C (edge yielded, but A already visited so recursion stops).
        AssertEdges(graph, ("B", "A"), ("C", "B"), ("A", "C"));
    }

    [Theory(DisplayName = "Traversal limits stop recursion")]
    [InlineData(2, 0, "MaxDepth")]
    [InlineData(0, 2, "MaxDefinitions")]
    public async Task BuildGraphAsync_WithTraversalLimit_StopsRecursion(int maxDepth, int maxDefinitions, string _)
    {
        SetupGraph(
            ("A", ["B"]),
            ("B", ["C"]),
            ("C", ["D"]),
            ("D", [])
        );

        var builder = CreateBuilder(new() { MaxDepth = maxDepth, MaxDefinitions = maxDefinitions });
        var graph = await builder.BuildGraphAsync("A");

        // Both limits produce 2 edges: B→A and C→B. D is never reached.
        AssertEdges(graph, ("B", "A"), ("C", "B"));
        Assert.DoesNotContain(graph.Edges, e => e.Source == "D");
    }

    [Fact]
    public async Task BuildGraphAsync_MultipleRoots_MergesGraphs()
    {
        SetupGraph(
            ("A", ["B", "C"]),
            ("B", ["C"]),
            ("C", [])
        );

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync(["A", "B"]);

        Assert.Equal(2, graph.RootDefinitionIds.Count);
        Assert.Contains("A", graph.RootDefinitionIds);
        Assert.Contains("B", graph.RootDefinitionIds);
        AssertConsumers(graph, "C");
    }

    [Fact]
    public async Task BuildGraphAsync_MultipleRoots_SharedConsumer_YieldsEdgesForEachRoot()
    {
        // X consumes both A and B.
        SetupGraph(
            ("A", ["X"]),
            ("B", ["X"]),
            ("X", [])
        );

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync(["A", "B"]);

        // Processing A: yields X→A, visits X. Processing B: yields X→B (edge yielded before visit check).
        Assert.Contains(graph.Edges, e => e is { Source: "X", Target: "A" });
        Assert.Contains(graph.Edges, e => e is { Source: "X", Target: "B" });
    }

    [Fact]
    public async Task BuildGraphAsync_OutboundLookup_ReturnsCorrectDependencies()
    {
        SetupGraph(
            ("A", ["B"]),
            ("B", [])
        );

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        var dependencies = graph.GetDependencies("B").ToList();
        Assert.Single(dependencies);
        Assert.Contains("A", dependencies);
    }

    [Fact]
    public async Task BuildGraphAsync_EmptyRootList_ReturnsEmptyGraph()
    {
        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync([]);

        Assert.Empty(graph.RootDefinitionIds);
        Assert.Empty(graph.Edges);
        Assert.Empty(graph.ConsumerDefinitionIds);
    }

    private WorkflowReferenceGraphBuilder CreateBuilder(WorkflowReferenceGraphOptions? options = null)
    {
        options ??= new();
        return new(_query, new OptionsWrapper<WorkflowReferenceGraphOptions>(options));
    }

    private void SetupGraph(params (string Source, string[] Consumers)[] definitions)
    {
        foreach (var (source, consumers) in definitions)
            _query.ExecuteAsync(source, Arg.Any<CancellationToken>()).Returns(consumers);
    }

    private static void AssertEdges(WorkflowReferenceGraph graph, params (string Source, string Target)[] edges)
    {
        Assert.Equal(edges.Length, graph.Edges.Count);
        foreach (var (source, target) in edges)
            Assert.Contains(graph.Edges, e => e is { Source: var s, Target: var t } && s == source && t == target);
    }

    private static void AssertConsumers(WorkflowReferenceGraph graph, params string[] consumerIds)
    {
        Assert.Equal(consumerIds.Length, graph.ConsumerDefinitionIds.Count);
        foreach (var consumerId in consumerIds)
            Assert.Contains(consumerId, graph.ConsumerDefinitionIds);
    }
}
