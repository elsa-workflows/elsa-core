using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Extensions;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents an activity in the context of an hierarchical tree structure, providing access to its siblings, parents and children.
/// </summary>
public class ActivityNode
{
    [JsonConstructor]
    public ActivityNode()
    {
    }

    public ActivityNode(JsonObject activity)
    {
        Activity = activity;
        NodeId = activity.GetNodeId();
    }

    public ActivityNode(JsonObject activity, string port) : this(activity) => Port = port;

    /// <summary>
    /// Gets the activity.
    /// </summary>
    public JsonObject Activity { get; set; } = default!;

    /// <summary>
    /// Gets the property name that contains the activity.
    /// </summary>
    public string? Port { get; set; }

    /// <summary>
    /// Gets the node ID.
    /// </summary>
    public string NodeId { get; set; } = default!;

    /// <summary>
    /// Gets the parents of this node.
    /// </summary>
    public ICollection<ActivityNode> Parents { get; set; } = new List<ActivityNode>();
    
    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    public ICollection<ActivityNode> Children { get; set; } = new List<ActivityNode>();

    /// <summary>
    /// Gets the descendants of this node.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ActivityNode> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;

            var descendants = child.Descendants();

            foreach (var descendant in descendants)
                yield return descendant;
        }
    }

    /// <summary>
    /// Gets the ancestors of this node.
    /// </summary>
    public IEnumerable<ActivityNode> Ancestors()
    {
        foreach (var parent in Parents)
        {
            yield return parent;

            var ancestors = parent.Ancestors();

            foreach (var ancestor in ancestors)
                yield return ancestor;
        }
    }

    /// <summary>
    /// Gets the siblings of this node.
    /// </summary>
    public IEnumerable<ActivityNode> Siblings() => Parents.SelectMany(parent => parent.Children);
    
    /// <summary>
    /// Gets the siblings and cousins of this node.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ActivityNode> SiblingsAndCousins() => Parents.SelectMany(parent => parent.Descendants());
}