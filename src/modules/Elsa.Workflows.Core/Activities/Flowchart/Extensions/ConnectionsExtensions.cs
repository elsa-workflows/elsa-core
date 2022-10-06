using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Extensions;

public static class ConnectionsExtensions
{
    public static IEnumerable<Connection> Descendants(this ICollection<Connection> allConnections, IActivity parent)
    {
        var children = allConnections.Where(x => parent == x.Source).ToList();

        foreach (var child in children)
        {
            yield return child;

            var descendants = allConnections.Descendants(child.Target).ToList();

            foreach (var descendant in descendants)
                yield return descendant;
        }
    }
    
    public static IEnumerable<Connection> InboundConnections(this ICollection<Connection> allConnections, IActivity activity) => 
        allConnections.Where(x => x.Target == activity).ToList();

    public static IEnumerable<IActivity> InboundActivities(this ICollection<Connection> allConnections, IActivity activity) => 
        allConnections.InboundConnections(activity).Select(x => x.Source);
}