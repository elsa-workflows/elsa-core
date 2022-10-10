using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Extensions;

public static class ConnectionsExtensions
{
    public static IEnumerable<Connection> Descendants(this ICollection<Connection> connections, IActivity parent)
    {
        var visitedActivities = new HashSet<IActivity>();
        return connections.Descendants(parent, visitedActivities);
    }
    
    public static IEnumerable<Connection> Ancestors(this ICollection<Connection> connections, IActivity activity)
    {
        var visitedActivities = new HashSet<IActivity>();
        return connections.Ancestors(activity, visitedActivities);
    }

    public static IEnumerable<Connection> InboundConnections(this ICollection<Connection> connections, IActivity activity) => connections.Where(x => x.Target == activity).ToList();

    public static IEnumerable<Connection> LeftInboundConnections(this ICollection<Connection> connections, IActivity activity)
    {
        // We only take "left" inbound connections, which means we exclude descendent connections looping back. 
        var descendantConnections = connections.Descendants(activity).ToList();
        var filteredConnections = connections.InboundConnections(activity).Except(descendantConnections).ToList();

        return filteredConnections;
    }
    
    public static IEnumerable<Connection> LeftAncestorConnections(this ICollection<Connection> connections, IActivity activity)
    {
        // We only take "left" inbound connections, which means we exclude descendent connections looping back. 
        var descendantConnections = connections.Descendants(activity).ToList();
        var filteredConnections = connections.Ancestors(activity).Except(descendantConnections).ToList();

        return filteredConnections;
    }

    public static IEnumerable<IActivity> InboundActivities(this ICollection<Connection> connections, IActivity activity) => connections.InboundConnections(activity).Select(x => x.Source);
    public static IEnumerable<IActivity> LeftInboundActivities(this ICollection<Connection> connections, IActivity activity) => connections.LeftInboundConnections(activity).Select(x => x.Source);
    public static IEnumerable<IActivity> LeftAncestorActivities(this ICollection<Connection> connections, IActivity activity) => connections.LeftAncestorConnections(activity).Select(x => x.Source);

    private static IEnumerable<Connection> Descendants(this ICollection<Connection> connections, IActivity parent, ISet<IActivity> visitedActivities)
    {
        var children = connections.Where(x => parent == x.Source && !visitedActivities.Contains(x.Target)).ToList();

        foreach (var child in children)
        {
            visitedActivities.Add(child.Target);
            yield return child;

            var descendants = connections.Descendants(child.Target, visitedActivities).ToList();

            foreach (var descendant in descendants)
            {
                yield return descendant;
            }
        }
    }
    
    private static IEnumerable<Connection> Ancestors(this ICollection<Connection> connections, IActivity activity, ISet<IActivity> visitedActivities)
    {
        var parents = connections.Where(x => activity == x.Target && !visitedActivities.Contains(x.Source)).ToList();

        foreach (var parent in parents)
        {
            visitedActivities.Add(parent.Source);
            yield return parent;

            var ancestors = connections.Ancestors(parent.Source, visitedActivities).ToList();

            foreach (var ancestor in ancestors)
            {
                yield return ancestor;
            }
        }
    }
}