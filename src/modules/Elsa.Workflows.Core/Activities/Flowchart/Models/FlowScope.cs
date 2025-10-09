using System.Text.Json.Serialization;

namespace Elsa.Workflows.Activities.Flowchart.Models;

/// <summary>
/// Represents a scope for tracking activity and connection visits within a flowchart execution.
/// </summary>
public class FlowScope
{
    [JsonConstructor]
    public FlowScope()
    {
    }

    [JsonInclude]
    private Dictionary<string, long> ActivitiesVisitCount { get; init; } = new();

    [JsonInclude]
    private Dictionary<string, long> ConnectionVisitCount { get; init; } = new();

    [JsonInclude]
    private Dictionary<string, bool> ConnectionLastVisitFollowed { get; init; } = new();

    /// <summary>
    /// Registers a visit to the specified activity, incrementing its visit count.
    /// </summary>
    /// <param name="activity">The activity being visited.</param>
    public void RegisterActivityVisit(IActivity activity)
    {
        string activityId = activity.Id;
        ActivitiesVisitCount.TryAdd(activityId, 0);
        ActivitiesVisitCount[activityId]++;
    }

    /// <summary>
    /// Gets the number of times the specified activity has been visited.
    /// </summary>
    /// <param name="activity">The activity to check.</param>
    /// <returns>The visit count of the activity.</returns>
    private long GetActivityVisitCount(IActivity activity) => ActivitiesVisitCount.GetValueOrDefault(activity.Id, 0);

    /// <summary>
    /// Registers a visit to the specified connection and records whether it was followed.
    /// </summary>
    /// <param name="connection">The connection being visited.</param>
    /// <param name="followed">Indicates whether the connection was followed.</param>
    public void RegisterConnectionVisit(Connection connection, bool followed)
    {
        var connectionId = connection.ToString();
        ConnectionVisitCount.TryAdd(connectionId, 0);
        ConnectionVisitCount[connectionId]++;
        ConnectionLastVisitFollowed[connectionId] = followed;
    }

    /// <summary>
    /// Gets the number of times the specified connection has been visited.
    /// </summary>
    /// <param name="connection">The connection to check.</param>
    /// <returns>The visit count of the connection.</returns>
    private long GetConnectionVisitCount(Connection connection) => ConnectionVisitCount.GetValueOrDefault(connection.ToString(), 0);

    /// <summary>
    /// Determines whether the last visit to the specified connection was followed.
    /// </summary>
    /// <param name="connection">The connection to check.</param>
    /// <returns>True if the connection was followed on the last visit, otherwise false.</returns>
    public bool GetConnectionLastVisitFollowed(Connection connection) => ConnectionLastVisitFollowed.GetValueOrDefault(connection.ToString(), false);

    /// <summary>
    /// Determines whether all inbound connections to the specified activity have been visited.
    /// </summary>
    /// <param name="flowGraph">The flow graph containing connections.</param>
    /// <param name="activity">The activity to check.</param>
    /// <returns>True if all inbound connections have been visited, otherwise false.</returns>
    public bool AllInboundConnectionsVisited(FlowGraph flowGraph, IActivity activity)
    {
        var forwardInboundConnections = flowGraph.GetForwardInboundConnections(activity);
        var outboundActivityVisitCount = GetActivityVisitCount(activity);
        var minConnectionVisitCount = forwardInboundConnections.Min(GetConnectionVisitCount);
        return minConnectionVisitCount > outboundActivityVisitCount;
    }

    /// <summary>
    /// Determines whether any inbound connection to the specified activity has been followed.
    /// </summary>
    /// <param name="flowGraph">The flow graph containing connections.</param>
    /// <param name="activity">The activity to check.</param>
    /// <returns>True if any inbound connection has been followed, otherwise false.</returns>
    public bool HasFollowedInboundConnection(FlowGraph flowGraph, IActivity activity)
    {
        var forwardInboundConnections = flowGraph.GetForwardInboundConnections(activity);
        var outboundActivityVisitCount = GetActivityVisitCount(activity);
        var maxConnectionVisitCount = forwardInboundConnections.Max(GetConnectionVisitCount);
        return maxConnectionVisitCount > outboundActivityVisitCount 
            && forwardInboundConnections.Any(c => GetConnectionVisitCount(c) == maxConnectionVisitCount && GetConnectionLastVisitFollowed(c));
    }

    /// <summary>
    /// Determines whether a connection should be ignored based on visit counts.
    /// </summary>
    /// <param name="connection">The connection to check.</param>
    /// <param name="activity">The activity associated with the connection.</param>
    /// <returns>True if the connection should be ignored, otherwise false.</returns>
    public bool ShouldIgnoreConnection(Connection connection, IActivity activity)
    {
        var connectionVisitCount = GetConnectionVisitCount(connection);
        var activityVisitCount = GetActivityVisitCount(activity);
        return connectionVisitCount <= activityVisitCount;
    }
}
