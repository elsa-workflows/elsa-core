using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents an activity in the context of an hierarchical tree structure, providing access to its siblings, parents and children.
/// </summary>
public class ActivityNode
{
    public ActivityNode(IActivity activity)
    {
        Activity = activity;
    }

    public string NodeId
    {
        get
        {
            var ancestorIds = Ancestors().Reverse().Select(x => x.Activity.Id).ToList();
            return ancestorIds.Any() ? $"{string.Join(":", ancestorIds)}:{Activity.Id}" : Activity.Id;
        }
    }

    public IActivity Activity { get; }
    public ICollection<ActivityNode> Parents { get; set; } = new List<ActivityNode>();
    public ICollection<ActivityNode> Children { get; set; } = new List<ActivityNode>();

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

    public IEnumerable<ActivityNode> Siblings() => Parents.SelectMany(parent => parent.Children);
    public IEnumerable<ActivityNode> SiblingsAndCousins() => Parents.SelectMany(parent => parent.Descendants());
}