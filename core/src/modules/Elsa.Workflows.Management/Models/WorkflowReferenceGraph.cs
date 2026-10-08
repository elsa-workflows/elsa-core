namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a single directed edge in the workflow reference graph.
/// The edge points from a consumer workflow (Source) to the workflow it references (Target).
/// </summary>
/// <param name="Source">The workflow definition ID of the consumer (the workflow that contains the reference).</param>
/// <param name="Target">The workflow definition ID being referenced (the dependency).</param>
public record WorkflowReferenceEdge(string Source, string Target);

/// <summary>
/// Represents a complete graph of workflow references, built by recursively traversing all consumers
/// of a given workflow definition.
/// </summary>
public class WorkflowReferenceGraph
{
    /// <summary>
    /// Creates a new instance of the <see cref="WorkflowReferenceGraph"/> class.
    /// </summary>
    /// <param name="rootDefinitionIds">The IDs of the root workflow definitions from which the graph was built.</param>
    /// <param name="edges">The collection of edges representing all reference relationships in the graph.</param>
    public WorkflowReferenceGraph(IReadOnlyCollection<string> rootDefinitionIds, IReadOnlyCollection<WorkflowReferenceEdge> edges)
    {
        RootDefinitionIds = rootDefinitionIds;
        Edges = edges;
        
        // Pre-compute useful lookups
        AllDefinitionIds = ComputeAllDefinitionIds(edges, rootDefinitionIds);
        ConsumerDefinitionIds = AllDefinitionIds.Except(rootDefinitionIds).ToHashSet();
        
        // Outbound: Source → Targets (what does this workflow depend on?)
        OutboundEdges = edges.ToLookup(e => e.Source, e => e.Target);
        
        // Inbound: Target → Sources (what workflows consume this one?)
        InboundEdges = edges.ToLookup(e => e.Target, e => e.Source);
    }

    /// <summary>
    /// The IDs of the root workflow definitions from which the graph was built.
    /// </summary>
    public IReadOnlyCollection<string> RootDefinitionIds { get; }

    /// <summary>
    /// The collection of edges representing all reference relationships in the graph.
    /// Each edge represents a single Source → Target relationship.
    /// </summary>
    public IReadOnlyCollection<WorkflowReferenceEdge> Edges { get; }

    /// <summary>
    /// All workflow definition IDs in the graph, including the roots and all consumers.
    /// </summary>
    public IReadOnlySet<string> AllDefinitionIds { get; }

    /// <summary>
    /// All workflow definition IDs that consume (directly or indirectly) the root workflow definitions.
    /// Does not include the roots themselves.
    /// </summary>
    public IReadOnlySet<string> ConsumerDefinitionIds { get; }

    /// <summary>
    /// A lookup that maps each workflow definition ID to the IDs of workflows it depends on (references).
    /// Use this to find: "What workflows does X reference?"
    /// </summary>
    public ILookup<string, string> OutboundEdges { get; }

    /// <summary>
    /// A lookup that maps each workflow definition ID to the IDs of workflows that reference it.
    /// Use this to find: "What workflows consume X?"
    /// </summary>
    public ILookup<string, string> InboundEdges { get; }

    /// <summary>
    /// Gets all workflow definition IDs that the specified workflow depends on (references).
    /// </summary>
    public IEnumerable<string> GetDependencies(string definitionId) => OutboundEdges[definitionId];

    /// <summary>
    /// Gets all workflow definition IDs that reference (consume) the specified workflow.
    /// </summary>
    public IEnumerable<string> GetConsumers(string definitionId) => InboundEdges[definitionId];

    private static HashSet<string> ComputeAllDefinitionIds(IReadOnlyCollection<WorkflowReferenceEdge> edges, IReadOnlyCollection<string> rootDefinitionIds)
    {
        var ids = new HashSet<string>(rootDefinitionIds);
        
        foreach (var edge in edges)
        {
            ids.Add(edge.Source);
            ids.Add(edge.Target);
        }
        
        return ids;
    }
}

