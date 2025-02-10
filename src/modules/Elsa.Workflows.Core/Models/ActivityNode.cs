namespace Elsa.Workflows.Models;

/// <summary>
/// Represents an activity in the context of an hierarchical tree structure, providing access to its siblings, parents and children.
/// </summary>
public class ActivityNode
{
    private readonly List<ActivityNode> _parents = new();
    private readonly List<ActivityNode> _children = new();
    private string? _nodeId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityNode"/> class.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="port">The port to which the activity belongs.</param>
    public ActivityNode(IActivity activity, string port)
    {
        Activity = activity;
        Port = port;
    }

    /// <summary>
    /// Gets the node ID.
    /// </summary>
    public string NodeId
    {
        get
        {
            if (_nodeId == null)
            {
                var ancestorIds = Ancestors().Reverse().Select(x => x.Activity.Id).ToList();
                _nodeId = ancestorIds.Any() ? $"{string.Join(":", ancestorIds)}:{Activity.Id}" : Activity.Id;
            }

            return _nodeId;
        }
    }

    /// <summary>
    /// Gets the activity.
    /// </summary>
    public IActivity Activity { get; }

    /// <summary>
    /// Gets the port to which the activity belongs.
    /// </summary>
    public string Port { get; }

    /// <summary>
    /// Gets the parents of this node.
    /// </summary>
    public IReadOnlyCollection<ActivityNode> Parents => _parents.AsReadOnly();

    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    public ICollection<ActivityNode> Children => _children.AsReadOnly();

    public void AddParent(ActivityNode parent)
    {
        _parents.Add(parent);
        _nodeId = null;
    }
    
    public void AddChild(ActivityNode child)
    {
        _children.Add(child);
    }

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