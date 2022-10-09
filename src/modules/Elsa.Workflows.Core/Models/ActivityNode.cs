using Elsa.Workflows.Core.Services;

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

    public string NodeId => Activity.Id;
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

    /// <summary>
    /// Traverses up the descendants until a single common ancestor is found (the fork).
    /// </summary>
    public ActivityNode? Fork()
    {
        var currentParents = Parents;

        while (currentParents.Any())
        {
            var grandParents = currentParents.SelectMany(x => x.Parents).Distinct().ToList();

            if (grandParents.Count == 1)
                return grandParents.Single();

            currentParents = grandParents;
        }

        return null;
    }

    /// <summary>
    /// Returns a list of nodes representing the ancestry from the current node to the specified common ancestry (aka the fork).
    /// </summary>
    public IEnumerable<ActivityNode> AncestryTo(ActivityNode fork)
    {
        var currentParents = Parents;

        while (currentParents.Any())
        {
            foreach (var parent in currentParents)
            {
                if (parent.Ancestors().Contains(fork))
                    yield return parent;

            }

            currentParents = currentParents.SelectMany(x => x.Parents).ToList();
        }
    }
}