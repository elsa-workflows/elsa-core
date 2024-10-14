using System.Security.Cryptography;
using System.Text;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Models;

/// <summary>
/// Hold a reference to a <see cref="Workflow"/> and a collection of <see cref="ActivityNode"/> instances representing the workflow.
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

    /// <summary>
    /// Gets the workflow.
    /// </summary>
    public Workflow Workflow { get; }

    /// <summary>
    /// Gets the root node.
    /// </summary>
    public ActivityNode Root { get; }

    /// <summary>
    /// Gets a flat collection of all nodes in the workflow.
    /// </summary>
    public ICollection<ActivityNode> Nodes { get; }

    /// <summary>
    /// Gets a lookup of nodes by their activity.
    /// </summary>
    public IDictionary<IActivity, ActivityNode> NodeActivityLookup { get; }

    /// <summary>
    /// Gets a lookup of nodes by their hash.
    /// </summary>
    public IDictionary<string, ActivityNode> NodeHashLookup { get; }

    /// <summary>
    /// Gets a lookup of nodes by their ID.
    /// </summary>
    public IDictionary<string, ActivityNode> NodeIdLookup { get; }

    private static string Hash(HashAlgorithm hashAlgorithm, string input)
    {
        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(data);
    }
}