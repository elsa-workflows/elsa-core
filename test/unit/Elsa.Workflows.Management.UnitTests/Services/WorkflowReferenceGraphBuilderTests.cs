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
        SetupFanOutGraph(); // A consumed by B and C.

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        // Edges.
        Assert.Equal(2, graph.Edges.Count);
        Assert.Contains(graph.Edges, e => e is { Source: "B", Target: "A" });
        Assert.Contains(graph.Edges, e => e is { Source: "C", Target: "A" });

        // Consumer IDs.
        Assert.Equal(2, graph.ConsumerDefinitionIds.Count);
        Assert.Contains("B", graph.ConsumerDefinitionIds);
        Assert.Contains("C", graph.ConsumerDefinitionIds);

        // Inbound lookup on target "A" returns the same consumers.
        var consumers = graph.GetConsumers("A").ToList();
        Assert.Equal(2, consumers.Count);
        Assert.Contains("B", consumers);
        Assert.Contains("C", consumers);
    }

    [Fact]
    public async Task BuildGraphAsync_WithTransitiveConsumers_ReturnsFullGraph()
    {
        _query.ExecuteAsync("A", Arg.Any<CancellationToken>()).Returns(["B"]);
        _query.ExecuteAsync("B", Arg.Any<CancellationToken>()).Returns(["C"]);
        _query.ExecuteAsync("C", Arg.Any<CancellationToken>()).Returns([]);

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        Assert.Equal(2, graph.Edges.Count);
        Assert.Contains(graph.Edges, e => e is { Source: "B", Target: "A" });
        Assert.Contains(graph.Edges, e => e is { Source: "C", Target: "B" });
        Assert.Equal(2, graph.ConsumerDefinitionIds.Count);
        Assert.Contains("B", graph.ConsumerDefinitionIds);
        Assert.Contains("C", graph.ConsumerDefinitionIds);
    }

    [Fact]
    public async Task BuildGraphAsync_WithDiamondGraph_IncludesAllEdges()
    {
        SetupDiamondGraph(); // A←{B,C}, B←D, C←D.

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        // All three consumers should be discovered.
        Assert.Equal(3, graph.ConsumerDefinitionIds.Count);
        Assert.Contains("B", graph.ConsumerDefinitionIds);
        Assert.Contains("C", graph.ConsumerDefinitionIds);
        Assert.Contains("D", graph.ConsumerDefinitionIds);

        // Edges: B→A, D→B (from B's branch), C→A, D→C (edge yielded before recursion is skipped).
        Assert.Equal(4, graph.Edges.Count);
        Assert.Contains(graph.Edges, e => e is { Source: "B", Target: "A" });
        Assert.Contains(graph.Edges, e => e is { Source: "D", Target: "B" });
        Assert.Contains(graph.Edges, e => e is { Source: "C", Target: "A" });
        Assert.Contains(graph.Edges, e => e is { Source: "D", Target: "C" });
    }

    [Fact]
    public async Task BuildGraphAsync_WithDiamondGraph_DoesNotRecurseAlreadyVisitedNodes()
    {
        SetupDiamondGraph(); // A←{B,C}, B←D, C←D.
        // Extend: D is also consumed by E.
        _query.ExecuteAsync("D", Arg.Any<CancellationToken>()).Returns(["E"]);
        _query.ExecuteAsync("E", Arg.Any<CancellationToken>()).Returns([]);

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
        // A → B → C → A (cycle of consumers).
        _query.ExecuteAsync("A", Arg.Any<CancellationToken>()).Returns(["B"]);
        _query.ExecuteAsync("B", Arg.Any<CancellationToken>()).Returns(["C"]);
        _query.ExecuteAsync("C", Arg.Any<CancellationToken>()).Returns(["A"]);

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync("A");

        // Edges: B→A, C→B, A→C (edge yielded, but A already visited so recursion stops).
        Assert.Equal(3, graph.Edges.Count);
        Assert.Contains(graph.Edges, e => e is { Source: "B", Target: "A" });
        Assert.Contains(graph.Edges, e => e is { Source: "C", Target: "B" });
        Assert.Contains(graph.Edges, e => e is { Source: "A", Target: "C" });
    }

    [Theory(DisplayName = "Traversal limits stop recursion")]
    [InlineData(2, 0, "MaxDepth")]
    [InlineData(0, 2, "MaxDefinitions")]
    public async Task BuildGraphAsync_WithTraversalLimit_StopsRecursion(int maxDepth, int maxDefinitions, string _)
    {
        // Chain: A → B → C → D (→ E when MaxDepth, but irrelevant — both limits truncate at the same point).
        _query.ExecuteAsync("A", Arg.Any<CancellationToken>()).Returns(["B"]);
        _query.ExecuteAsync("B", Arg.Any<CancellationToken>()).Returns(["C"]);
        _query.ExecuteAsync("C", Arg.Any<CancellationToken>()).Returns(["D"]);
        _query.ExecuteAsync("D", Arg.Any<CancellationToken>()).Returns([]);

        var builder = CreateBuilder(new() { MaxDepth = maxDepth, MaxDefinitions = maxDefinitions });
        var graph = await builder.BuildGraphAsync("A");

        // Both limits produce 2 edges: B→A and C→B. D is never reached.
        Assert.Equal(2, graph.Edges.Count);
        Assert.Contains(graph.Edges, e => e is { Source: "B", Target: "A" });
        Assert.Contains(graph.Edges, e => e is { Source: "C", Target: "B" });
        Assert.DoesNotContain(graph.Edges, e => e.Source == "D");
    }

    [Fact]
    public async Task BuildGraphAsync_MultipleRoots_MergesGraphs()
    {
        _query.ExecuteAsync("A", Arg.Any<CancellationToken>()).Returns(["B", "C"]);
        _query.ExecuteAsync("B", Arg.Any<CancellationToken>()).Returns(["C"]);
        _query.ExecuteAsync("C", Arg.Any<CancellationToken>()).Returns([]);

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync(["A", "B"]);

        Assert.Equal(2, graph.RootDefinitionIds.Count);
        Assert.Contains("A", graph.RootDefinitionIds);
        Assert.Contains("B", graph.RootDefinitionIds);
        Assert.Single(graph.ConsumerDefinitionIds);
        Assert.Contains("C", graph.ConsumerDefinitionIds);
    }

    [Fact]
    public async Task BuildGraphAsync_MultipleRoots_SharedConsumer_YieldsEdgesForEachRoot()
    {
        // X consumes both A and B.
        _query.ExecuteAsync("A", Arg.Any<CancellationToken>()).Returns(["X"]);
        _query.ExecuteAsync("B", Arg.Any<CancellationToken>()).Returns(["X"]);
        _query.ExecuteAsync("X", Arg.Any<CancellationToken>()).Returns([]);

        var builder = CreateBuilder();
        var graph = await builder.BuildGraphAsync(["A", "B"]);

        // Processing A: yields X→A, visits X. Processing B: yields X→B (edge yielded before visit check).
        Assert.Contains(graph.Edges, e => e is { Source: "X", Target: "A" });
        Assert.Contains(graph.Edges, e => e is { Source: "X", Target: "B" });
    }

    [Fact]
    public async Task BuildGraphAsync_OutboundLookup_ReturnsCorrectDependencies()
    {
        _query.ExecuteAsync("A", Arg.Any<CancellationToken>()).Returns(["B"]);
        _query.ExecuteAsync("B", Arg.Any<CancellationToken>()).Returns([]);

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

    /// <summary>A consumed by B and C (fan-out). B and C are leaves.</summary>
    private void SetupFanOutGraph()
    {
        _query.ExecuteAsync("A", Arg.Any<CancellationToken>()).Returns(["B", "C"]);
        _query.ExecuteAsync("B", Arg.Any<CancellationToken>()).Returns([]);
        _query.ExecuteAsync("C", Arg.Any<CancellationToken>()).Returns([]);
    }

    /// <summary>Diamond: A←{B,C}, B←D, C←D. D is a leaf by default.</summary>
    private void SetupDiamondGraph()
    {
        _query.ExecuteAsync("A", Arg.Any<CancellationToken>()).Returns(["B", "C"]);
        _query.ExecuteAsync("B", Arg.Any<CancellationToken>()).Returns(["D"]);
        _query.ExecuteAsync("C", Arg.Any<CancellationToken>()).Returns(["D"]);
        _query.ExecuteAsync("D", Arg.Any<CancellationToken>()).Returns([]);
    }
}
