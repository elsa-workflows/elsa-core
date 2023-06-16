using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Extensions;

/// <summary>
/// Contains extension methods for <see cref="ICollection{Connection}"/>.
/// </summary>
public static class ConnectionsExtensions
{
    /// <summary>
    /// Returns all connections that are descendants of the specified parent activity.
    /// </summary>
    public static IEnumerable<Connection> Descendants(this ICollection<Connection> connections, IActivity parent)
    {
        var visitedActivities = new HashSet<IActivity>();
        return connections.Descendants(parent, visitedActivities);
    }
    
    /// <summary>
    /// Returns all ancestor connections of the specified parent activity.
    /// </summary>
    public static IEnumerable<Connection> Ancestors(this ICollection<Connection> connections, IActivity activity)
    {
        var visitedActivities = new HashSet<IActivity>();
        return connections.Ancestors(activity, visitedActivities);
    }

    /// <summary>
    /// Returns all inbound connections of the specified activity.
    /// </summary>
    public static IEnumerable<Connection> InboundConnections(this ICollection<Connection> connections, IActivity activity) => connections.Where(x => x.Target.Activity == activity).ToList();

    /// <summary>
    /// Returns all "left" inbound connections of the specified activity. "Left" means "not a descendant of the activity".
    /// </summary>
    public static IEnumerable<Connection> LeftInboundConnections(this ICollection<Connection> connections, IActivity activity)
    {
        // We only take "left" inbound connections, which means we exclude descendent connections looping back. 
        var descendantConnections = connections.Descendants(activity).ToList();
        var filteredConnections = connections.InboundConnections(activity).Except(descendantConnections).ToList();

        return filteredConnections;
    }
    
    /// <summary>
    /// Returns all "left" ancestor connections of the specified activity. "Left" means "not a descendant of the activity".
    /// </summary>
    public static IEnumerable<Connection> LeftAncestorConnections(this ICollection<Connection> connections, IActivity activity)
    {
        // We only take "left" inbound connections, which means we exclude descendent connections looping back. 
        var descendantConnections = connections.Descendants(activity).ToList();
        var filteredConnections = connections.Ancestors(activity).Except(descendantConnections).ToList();

        return filteredConnections;
    }

    /// <summary>
    /// Returns all inbound activities of the specified activity.
    /// </summary>
    public static IEnumerable<IActivity> InboundActivities(this ICollection<Connection> connections, IActivity activity) => connections.InboundConnections(activity).Select(x => x.Source.Activity);
    
    /// <summary>
    /// Returns all "left" inbound activities of the specified activity. "Left" means "not a descendant of the activity".
    /// </summary>
    public static IEnumerable<IActivity> LeftInboundActivities(this ICollection<Connection> connections, IActivity activity) => connections.LeftInboundConnections(activity).Select(x => x.Source.Activity);
    
    /// <summary>
    /// Returns all "left" ancestor activities of the specified activity. "Left" means "not a descendant of the activity".
    /// </summary>
    public static IEnumerable<IActivity> LeftAncestorActivities(this ICollection<Connection> connections, IActivity activity) => connections.LeftAncestorConnections(activity).Select(x => x.Source.Activity);

    private static IEnumerable<Connection> Descendants(this ICollection<Connection> connections, IActivity parent, ISet<IActivity> visitedActivities)
    {
        var children = connections.Where(x => parent == x.Source.Activity && !visitedActivities.Contains(x.Target.Activity)).ToList();

        foreach (var child in children)
        {
            visitedActivities.Add(child.Target.Activity);
            yield return child;

            var descendants = connections.Descendants(child.Target.Activity, visitedActivities).ToList();

            foreach (var descendant in descendants)
            {
                yield return descendant;
            }
        }
    }
    
    private static IEnumerable<Connection> Ancestors(this ICollection<Connection> connections, IActivity activity, ISet<IActivity> visitedActivities)
    {
        var parents = connections.Where(x => activity == x.Target.Activity && !visitedActivities.Contains(x.Source.Activity)).ToList();

        foreach (var parent in parents)
        {
            visitedActivities.Add(parent.Source.Activity);
            yield return parent;

            var ancestors = connections.Ancestors(parent.Source.Activity, visitedActivities).ToList();

            foreach (var ancestor in ancestors)
            {
                yield return ancestor;
            }
        }
    }
}