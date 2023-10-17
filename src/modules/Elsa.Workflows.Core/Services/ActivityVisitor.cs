using Elsa.Workflows.Core.Contracts;
using ActivityNode = Elsa.Workflows.Core.Models.ActivityNode;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ActivityVisitor : IActivityVisitor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IActivityResolver> _portResolvers;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityVisitor(IEnumerable<IActivityResolver> portResolvers, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _portResolvers = portResolvers.OrderByDescending(x => x.Priority).ToList();
    }

    /// <inheritdoc />
    public async Task<ActivityNode> VisitAsync(IActivity activity, bool useActivityIdAsNodeId, CancellationToken cancellationToken = default)
    {
        var collectedActivities = new HashSet<IActivity>(new[] { activity });
        var graph = new ActivityNode(activity, useActivityIdAsNodeId);
        var collectedNodes = new HashSet<ActivityNode>(new[] { graph });
        await VisitRecursiveAsync((graph, activity), collectedActivities, collectedNodes, useActivityIdAsNodeId, cancellationToken);
        return graph;
    }

    private async Task VisitRecursiveAsync((ActivityNode Node, IActivity Activity) pair, HashSet<IActivity> collectedActivities, HashSet<ActivityNode> collectedNodes, bool useActivityIdAsNodeId, CancellationToken cancellationToken)
    {
        if (pair.Activity is IInitializable initializable)
        {
            var context = new InitializationContext(_serviceProvider, cancellationToken);
            await initializable.InitializeAsync(context);
        }

        await VisitPortsRecursiveAsync(pair, collectedActivities, collectedNodes, useActivityIdAsNodeId, cancellationToken);
    }

    private async Task VisitPortsRecursiveAsync((ActivityNode Node, IActivity Activity) pair, HashSet<IActivity> collectedActivities, HashSet<ActivityNode> collectedNodes, bool useActivityIdAsNodeId, CancellationToken cancellationToken)
    {
        var resolver = _portResolvers.FirstOrDefault(x => x.GetSupportsActivity(pair.Activity));

        if (resolver == null)
            return;

        var ports = await resolver.GetActivitiesAsync(pair.Activity, cancellationToken);

        foreach (var port in ports)
        {
            // Continue if the specified activity was already encountered.
            if (collectedActivities.Contains(port))
                continue;

            var childNode = collectedNodes.FirstOrDefault(x => x.Activity == port);

            if (childNode == null)
            {
                childNode = new ActivityNode(port, useActivityIdAsNodeId);
                collectedNodes.Add(childNode);
            }

            childNode.Parents.Add(pair.Node);
            pair.Node.Children.Add(childNode);
            collectedActivities.Add(port);
            await VisitRecursiveAsync((childNode, port), collectedActivities, collectedNodes, useActivityIdAsNodeId, cancellationToken);
        }
    }
}