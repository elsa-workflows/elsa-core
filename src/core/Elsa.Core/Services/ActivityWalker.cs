using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Services;

public class ActivityWalker : IActivityWalker
{
    private readonly IEnumerable<IActivityNodeResolver> _portResolvers;

    public ActivityWalker(IEnumerable<IActivityNodeResolver> portResolvers)
    {
        _portResolvers = portResolvers.OrderByDescending(x => x.Priority).ToList();
    }

    public ActivityNode Walk(IActivity activity)
    {
        var collectedActivities = new HashSet<IActivity>(new[] { activity });
        var graph = new ActivityNode(activity);
        var collectedNodes = new HashSet<ActivityNode>(new[] { graph });
        WalkRecursive((graph, activity), collectedActivities, collectedNodes);
        return graph;
    }

    private void WalkRecursive((ActivityNode Node, IActivity Activity) pair, HashSet<IActivity> collectedActivities, HashSet<ActivityNode> collectedNodes)
    {
        WalkPortsRecursive(pair, collectedActivities, collectedNodes);
    }

    private void WalkPortsRecursive((ActivityNode Node, IActivity Activity) pair, HashSet<IActivity> collectedActivities, HashSet<ActivityNode> collectedNodes)
    {
        var resolver = _portResolvers.FirstOrDefault(x => x.GetSupportsActivity(pair.Activity));

        if (resolver == null)
            return;

        var ports = resolver.GetPorts(pair.Activity);

        foreach (var port in ports)
        {
            var childNode = collectedNodes.FirstOrDefault(x => x.Activity == port);

            if (childNode == null)
            {
                childNode = new ActivityNode(port);
                collectedNodes.Add(childNode);
            }

            childNode.Parents.Add(pair.Node);
            pair.Node.Children.Add(childNode);
            collectedActivities.Add(port);
            WalkRecursive((childNode, port), collectedActivities, collectedNodes);
        }
    }
}