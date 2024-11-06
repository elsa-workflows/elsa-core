using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents an activity in the context of an hierarchical tree structure, providing access to its siblings, parents and children.
/// </summary>
public class WorkflowGraphNode(WorkflowGraph workflowGraph)
{
    /// <summary>
    /// Gets the workflow graph associated with this node.
    /// </summary>
    public WorkflowGraph WorkflowGraph { get; } = workflowGraph;
    
    /// <summary>
    /// Gets the parents of this node.
    /// </summary>
    public HashSet<WorkflowGraphNode> Predecessors { get; set; } = new();
    
    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    public HashSet<WorkflowGraphNode> Successors { get; set; } = new();
}