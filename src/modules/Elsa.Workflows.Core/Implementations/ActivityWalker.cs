using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Services;
using ActivityNode = Elsa.Workflows.Core.Models.ActivityNode;

namespace Elsa.Workflows.Core.Implementations;

public class ActivityWalker : IActivityWalker
{
    private readonly IEnumerable<IActivityPortResolver> _portResolvers;

    public ActivityWalker(IEnumerable<IActivityPortResolver> portResolvers)
    {
        _portResolvers = portResolvers.OrderByDescending(x => x.Priority).ToList();
    }

    public async Task<ActivityNode> WalkAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var collectedActivities = new HashSet<IActivity>(new[] { activity });
        var graph = new ActivityNode(activity);
        var collectedNodes = new HashSet<ActivityNode>(new[] { graph });
        await WalkRecursiveAsync((graph, activity), collectedActivities, collectedNodes, cancellationToken);
        return graph;
    }

    private async Task WalkRecursiveAsync((ActivityNode Node, IActivity Activity) pair, HashSet<IActivity> collectedActivities, HashSet<ActivityNode> collectedNodes, CancellationToken cancellationToken)
    {
        await WalkPortsRecursiveAsync(pair, collectedActivities, collectedNodes, cancellationToken);
    }

    private async Task WalkPortsRecursiveAsync((ActivityNode Node, IActivity Activity) pair, HashSet<IActivity> collectedActivities, HashSet<ActivityNode> collectedNodes, CancellationToken cancellationToken)
    {
        var resolver = _portResolvers.FirstOrDefault(x => x.GetSupportsActivity(pair.Activity));

        if (resolver == null)
            return;

        var ports = await resolver.GetPortsAsync(pair.Activity, cancellationToken);

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
            await WalkRecursiveAsync((childNode, port), collectedActivities, collectedNodes, cancellationToken);
        }
    }
}