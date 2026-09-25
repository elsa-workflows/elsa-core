using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class DefaultActivityVisitor : IActivityVisitor
{
    private readonly IEnumerable<IActivityResolver> _portResolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultActivityVisitor"/> class.
    /// </summary>
    public DefaultActivityVisitor(IEnumerable<IActivityResolver> portResolvers)
    {
        _portResolvers = portResolvers.OrderByDescending(x => x.Priority).ToList();
    }

    /// <inheritdoc />
    public async Task<ActivityNode> VisitAsync(JsonObject activity, CancellationToken cancellationToken = default)
    {
        var collectedActivities = new HashSet<JsonObject>(new[] { activity });
        var graph = new ActivityNode(activity);
        var collectedNodes = new HashSet<ActivityNode>(new[] { graph });
        await VisitRecursiveAsync((graph, activity), collectedActivities, collectedNodes, cancellationToken);
        return graph;
    }

    private async Task VisitRecursiveAsync((ActivityNode Node, JsonObject Activity) pair, HashSet<JsonObject> collectedActivities, HashSet<ActivityNode> collectedNodes, CancellationToken cancellationToken)
    {
        await VisitPortsRecursiveAsync(pair, collectedActivities, collectedNodes, cancellationToken);
    }

    private async Task VisitPortsRecursiveAsync((ActivityNode Node, JsonObject Activity) pair, HashSet<JsonObject> collectedActivities, HashSet<ActivityNode> collectedNodes, CancellationToken cancellationToken)
    {
        var resolver = _portResolvers.FirstOrDefault(x => x.GetSupportsActivity(pair.Activity));

        if (resolver == null)
            return;

        var embeddedActivities = await resolver.GetActivitiesAsync(pair.Activity, cancellationToken);

        foreach (var embeddedActivity in embeddedActivities)
        {
            // Continue if the specified activity was already encountered.
            if (collectedActivities.Contains(embeddedActivity.Activity))
                continue;

            var childNode = collectedNodes.FirstOrDefault(x => x.Activity == embeddedActivity.Activity);

            if (childNode == null)
            {
                childNode = new ActivityNode(embeddedActivity.Activity, embeddedActivity.PropertyName);
                collectedNodes.Add(childNode);
            }

            childNode.Parents.Add(pair.Node);
            pair.Node.Children.Add(childNode);
            collectedActivities.Add(embeddedActivity.Activity);
            await VisitRecursiveAsync((childNode, embeddedActivity.Activity), collectedActivities, collectedNodes, cancellationToken);
        }
    }
}