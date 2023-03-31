using Elsa.Workflows.Core.Contracts;
using ActivityNode = Elsa.Workflows.Core.Models.ActivityNode;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ActivityVisitor : IActivityVisitor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IActivityPortResolver> _portResolvers;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityVisitor(IEnumerable<IActivityPortResolver> portResolvers, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _portResolvers = portResolvers.OrderByDescending(x => x.Priority).ToList();
    }

    /// <inheritdoc />
    public async Task<ActivityNode> VisitAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var collectedActivities = new HashSet<IActivity>(new[] { activity });
        var graph = new ActivityNode(activity);
        var collectedNodes = new HashSet<ActivityNode>(new[] { graph });
        await VisitRecursiveAsync((graph, activity), collectedActivities, collectedNodes, cancellationToken);
        return graph;
    }

    private async Task VisitRecursiveAsync((ActivityNode Node, IActivity Activity) pair, HashSet<IActivity> collectedActivities, HashSet<ActivityNode> collectedNodes, CancellationToken cancellationToken)
    {
        if (pair.Activity is IInitializable initializable)
        {
            var context = new InitializationContext(_serviceProvider, cancellationToken);
            await initializable.InitializeAsync(context);
        }

        await VisitPortsRecursiveAsync(pair, collectedActivities, collectedNodes, cancellationToken);
    }

    private async Task VisitPortsRecursiveAsync((ActivityNode Node, IActivity Activity) pair, HashSet<IActivity> collectedActivities, HashSet<ActivityNode> collectedNodes, CancellationToken cancellationToken)
    {
        var resolver = _portResolvers.FirstOrDefault(x => x.GetSupportsActivity(pair.Activity));

        if (resolver == null)
            return;

        var ports = await resolver.GetPortsAsync(pair.Activity, cancellationToken);

        foreach (var port in ports)
        {
            // Continue if the specified activity was already encountered.
            if (collectedActivities.Contains(port))
                continue;

            var childNode = collectedNodes.FirstOrDefault(x => x.Activity == port);

            if (childNode == null)
            {
                childNode = new ActivityNode(port);
                collectedNodes.Add(childNode);
            }

            childNode.Parents.Add(pair.Node);
            pair.Node.Children.Add(childNode);
            collectedActivities.Add(port);
            await VisitRecursiveAsync((childNode, port), collectedActivities, collectedNodes, cancellationToken);
        }
    }
}