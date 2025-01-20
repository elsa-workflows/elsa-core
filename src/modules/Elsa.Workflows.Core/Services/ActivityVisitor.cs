using ActivityNode = Elsa.Workflows.Models.ActivityNode;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityVisitor : IActivityVisitor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IActivityResolver> _portResolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityVisitor"/> class.
    /// </summary>
    public ActivityVisitor(IEnumerable<IActivityResolver> portResolvers, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _portResolvers = portResolvers.OrderByDescending(x => x.Priority).ToList();
    }

    /// <inheritdoc />
    public async Task<ActivityNode> VisitAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var graph = new ActivityNode(activity, "Root");
        var collectedNodes = new HashSet<ActivityNode>([
            graph
        ]);
        var collectedActivities = new HashSet<IActivity>([
            activity
        ]);
        var visitorContext = new ActivityVisitorContext
        {
            CollectedActivities = collectedActivities,
            CollectedNodes = collectedNodes
        };

        await VisitRecursiveAsync((graph, activity), visitorContext, cancellationToken);
        return graph;
    }

    private async Task VisitRecursiveAsync((ActivityNode Node, IActivity Activity) pair, ActivityVisitorContext visitorContext, CancellationToken cancellationToken)
    {
        if (pair.Activity is IInitializable initializable)
        {
            var context = new InitializationContext(_serviceProvider, cancellationToken);
            await initializable.InitializeAsync(context);
        }

        await VisitPortsRecursiveAsync(pair, visitorContext, cancellationToken);
    }

    private async Task VisitPortsRecursiveAsync((ActivityNode Node, IActivity Activity) pair, ActivityVisitorContext visitorContext, CancellationToken cancellationToken)
    {
        var resolver = _portResolvers.FirstOrDefault(x => x.GetSupportsActivity(pair.Activity));

        if (resolver == null)
            return;

        var activityPorts = await resolver.GetActivityPortsAsync(pair.Activity, cancellationToken);
        var collectedActivities = visitorContext.CollectedActivities;
        var collectedNodes = visitorContext.CollectedNodes;

        foreach (var activityPort in activityPorts)
        {
            foreach (var activity in activityPort.GetActivities())
            {
                // Continue if the specified activity was already encountered.
                if (collectedActivities.Contains(activity))
                    continue;

                var childNode = collectedNodes.FirstOrDefault(x => x.Activity == activity);

                if (childNode == null)
                {
                    childNode = new(activity, activityPort.PortName);
                    collectedNodes.Add(childNode);
                }

                childNode.AddParent(pair.Node);
                pair.Node.AddChild(childNode);
                collectedActivities.Add(activity);
                await VisitRecursiveAsync((childNode, activity), visitorContext, cancellationToken);
            }
        }
    }

    private class ActivityVisitorContext
    {
        public HashSet<IActivity> CollectedActivities { get; set; } = [];
        public HashSet<ActivityNode> CollectedNodes { get; set; } = [];
    }
}