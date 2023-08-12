using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents an activity in the context of an hierarchical tree structure, providing access to its siblings, parents and children.
/// </summary>
public class ActivityNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityNode"/> class.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="useActivityIdAsNodeId">Whether to use the activity ID as the node ID.</param>
    public ActivityNode(IActivity activity, bool useActivityIdAsNodeId)
    {
        Activity = activity;
        UseActivityIdAsNodeId = useActivityIdAsNodeId;
    }

    /// <summary>
    /// Gets the node ID.
    /// </summary>
    public string NodeId
    {
        get
        {
            if (UseActivityIdAsNodeId)
                return Activity.Id;
            
            var ancestorIds = Ancestors().Reverse().Select(x => x.Activity.Id).ToList();
            return ancestorIds.Any() ? $"{string.Join(":", ancestorIds)}:{Activity.Id}" : Activity.Id;
        }
    }

    /// <summary>
    /// Gets the activity.
    /// </summary>
    public IActivity Activity { get; }
    
    /// <summary>
    /// Gets a value indicating whether to use the activity ID as the node ID.
    /// </summary>
    public bool UseActivityIdAsNodeId { get; }
    
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