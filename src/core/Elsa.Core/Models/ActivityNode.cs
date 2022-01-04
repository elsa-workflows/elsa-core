using Elsa.Contracts;

namespace Elsa.Models;

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
}