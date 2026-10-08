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

    /// <summary>
    /// Finds the activity based on the provided <paramref name="handle"/>.
    /// </summary>
    /// <param name="handle">The handle containing the identification parameters for the activity.</param>
    /// <returns>The activity found based on the handle, or null if no activity is found.</returns>
    public IActivity? FindActivity(ActivityHandle handle)
    {
        return handle.ActivityId != null
            ? FindActivityById(handle.ActivityId)
            : handle.ActivityNodeId != null
                ? FindActivityByNodeId(handle.ActivityNodeId)
                : handle.ActivityHash != null
                    ? FindActivityByHash(handle.ActivityHash)
                    : null;
    }

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> with the specified activity ID from the workflow graph.
    /// </summary>
    public ActivityNode? FindNodeById(string nodeId) => NodeIdLookup.TryGetValue(nodeId, out var node) ? node : null;

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> with the specified hash of the activity node ID from the workflow graph.
    /// </summary>
    /// <param name="hash">The hash of the activity node ID.</param>
    /// <returns>The <see cref="ActivityNode"/> with the specified hash of the activity node ID.</returns>
    public ActivityNode? FindNodeByHash(string hash) => NodeHashLookup.TryGetValue(hash, out var node) ? node : null;

    /// Returns the <see cref="ActivityNode"/> containing the specified activity from the workflow graph.
    public ActivityNode? FindNodeByActivity(IActivity activity)
    {
        return NodeActivityLookup.TryGetValue(activity, out var node) ? node : null;
    }

    /// Returns the <see cref="ActivityNode"/> associated with the specified activity ID.
    public ActivityNode? FindNodeByActivityId(string activityId) => Nodes.FirstOrDefault(x => x.Activity.Id == activityId);

    /// Returns the <see cref="IActivity"/> with the specified ID from the workflow graph.
    public IActivity? FindActivityByNodeId(string nodeId) => FindNodeById(nodeId)?.Activity;

    /// Returns the <see cref="IActivity"/> with the specified ID from the workflow graph.
    public IActivity? FindActivityById(string activityId) => FindNodeById(NodeIdLookup.SingleOrDefault(n => n.Key.EndsWith(activityId)).Value.NodeId)?.Activity;

    /// Returns the <see cref="IActivity"/> with the specified hash of the activity node ID from the workflow graph.
    /// <param name="hash">The hash of the activity node ID.</param>
    /// <returns>The <see cref="IActivity"/> with the specified hash of the activity node ID.</returns>
    public IActivity? FindActivityByHash(string hash) => FindNodeByHash(hash)?.Activity;

    private static string Hash(HashAlgorithm hashAlgorithm, string input)
    {
        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(data);
    }
}