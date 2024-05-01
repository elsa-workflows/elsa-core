using System.Security.Cryptography;
using System.Text;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Models;

/// <summary>
/// Represents a workflow graph, which is a collection of activity nodes that form a directed graph.
/// </summary>
public record WorkflowGraph
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowGraph"/> class.
    /// </summary>
    public WorkflowGraph(Workflow workflow, ActivityNode root, IEnumerable<ActivityNode> nodes)
    {
        using var hashAlgorithm = SHA256.Create();
        Workflow = workflow;
        Root = root;
        Nodes = nodes.ToList();
        NodeIdLookup = Nodes.ToDictionary(x => x.NodeId);
        NodeHashLookup = Nodes.ToDictionary(x => Hash(hashAlgorithm, x.NodeId));
        NodeActivityLookup = Nodes.ToDictionary(x => x.Activity);
    }

    public Workflow Workflow { get; }
    public ActivityNode Root { get; }
    public ICollection<ActivityNode> Nodes { get; }
    public IDictionary<IActivity, ActivityNode> NodeActivityLookup { get; }
    public IDictionary<string, ActivityNode> NodeHashLookup { get; }
    public IDictionary<string, ActivityNode> NodeIdLookup { get; }

    private static string Hash(HashAlgorithm hashAlgorithm, string input)
    {
        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(data);
    }
}