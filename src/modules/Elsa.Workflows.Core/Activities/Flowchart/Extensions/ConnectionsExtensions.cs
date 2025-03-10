using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

/// <summary>
/// Contains extension methods for <see cref="ICollection{Connection}"/>.
/// </summary>
public static class ConnectionsExtensions
{
   /// <summary>
    /// Returns all inbound connections of the specified activity.
    /// </summary>
    public static IEnumerable<Connection> InboundConnections(this ICollection<Connection> connections, IActivity activity) => connections.Where(x => x.Target.Activity == activity).Distinct().ToList();

    /// <summary>
    /// Returns all inbound activities of the specified activity.
    /// </summary>
    public static IEnumerable<IActivity> InboundActivities(this ICollection<Connection> connections, IActivity activity) => connections.InboundConnections(activity).Select(x => x.Source.Activity);

    /// <summary>
    /// Returns all outbound connections of the specified activity.
    /// </summary>
    public static IEnumerable<Connection> OutboundConnections(this ICollection<Connection> connections, IActivity activity) => connections.Where(x => x.Source.Activity == activity).Distinct().ToList();

    /// <summary>
    /// Returns all outbound connections of the specified activity matching the specified outcomes.
    /// </summary>
    public static IEnumerable<Connection> OutboundConnections(this ICollection<Connection> connections, IActivity activity, Outcomes outcomes) => connections.OutboundConnections(activity).Where(c => outcomes.Names.Contains(c.Source.Port));

    /// <summary>
    /// Returns all outbound activities of the specified activity.
    /// </summary>
    public static IEnumerable<IActivity> OutboundActivities(this ICollection<Connection> connections, IActivity activity) => connections.OutboundConnections(activity).Select(x => x.Source.Activity);

    /// <summary>
    /// Returns all outbound activities of the specified activity matching the specified outcomes
    /// </summary>
    public static IEnumerable<IActivity> OutboundActivities(this ICollection<Connection> connections, IActivity activity, Outcomes outcomes) => connections.OutboundConnections(activity).Where(c => outcomes.Names.Contains(c.Source.Port)).Select(x => x.Source.Activity);
}