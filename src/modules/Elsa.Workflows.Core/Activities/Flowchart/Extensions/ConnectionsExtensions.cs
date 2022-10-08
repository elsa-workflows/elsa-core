using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Extensions;

public static class ConnectionsExtensions
{
    public static IEnumerable<Connection> Descendants(this ICollection<Connection> allConnections, IActivity parent)
    {
        var visitedActivities = new HashSet<IActivity>();
        return allConnections.Descendants(parent, visitedActivities);
    }

    public static IEnumerable<Connection> InboundConnections(this ICollection<Connection> allConnections, IActivity activity) => allConnections.Where(x => x.Target == activity).ToList();

    public static IEnumerable<Connection> LeftInboundConnections(this ICollection<Connection> allConnections, IActivity activity)
    {
        // We only take "left" inbound connections, which means we exclude descendent connections looping back. 
        var descendantConnections = allConnections.Descendants(activity).ToList();
        var filteredConnections = allConnections.InboundConnections(activity).Except(descendantConnections).ToList();

        return filteredConnections;
    }

    public static IEnumerable<IActivity> InboundActivities(this ICollection<Connection> allConnections, IActivity activity) => allConnections.InboundConnections(activity).Select(x => x.Source);
    public static IEnumerable<IActivity> LeftInboundActivities(this ICollection<Connection> allConnections, IActivity activity) => allConnections.LeftInboundConnections(activity).Select(x => x.Source);

    private static IEnumerable<Connection> Descendants(this ICollection<Connection> allConnections, IActivity parent, ISet<IActivity> visitedActivities)
    {
        var children = allConnections.Where(x => parent == x.Source && !visitedActivities.Contains(x.Target)).ToList();

        foreach (var child in children)
        {
            visitedActivities.Add(child.Target);
            yield return child;

            var descendants = allConnections.Descendants(child.Target, visitedActivities).ToList();

            foreach (var descendant in descendants)
            {
                yield return descendant;
            }
        }
    }
}