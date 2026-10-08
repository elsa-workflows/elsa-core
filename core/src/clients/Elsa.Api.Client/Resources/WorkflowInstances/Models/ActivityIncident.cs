using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// Holds information about an activity incident.
/// </summary>
public class ActivityIncident
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityIncident"/> class.
    /// </summary>
    [JsonConstructor]
    public ActivityIncident()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityIncident"/> class.
    /// </summary>
    /// <param name="activityId">The ID of the activity that caused the incident.</param>
    /// <param name="activityNodeId">The Node ID of the activity that caused the incident.</param>
    /// <param name="activityType">The type of the activity that caused the incident.</param>
    /// <param name="message">The message of the incident.</param>
    /// <param name="exception">The exception that caused the incident.</param>
    /// <param name="timestamp">The timestamp of the incident.</param>
    public ActivityIncident(string activityId, string activityNodeId, string activityType, string message, ExceptionState? exception, DateTimeOffset timestamp)
    {
        ActivityId = activityId;
        ActivityNodeId = activityNodeId;
        ActivityType = activityType;
        Message = message;
        Exception = exception;
        Timestamp = timestamp;
    }

    /// <summary>The ID of the activity that caused the incident.</summary>
    public string ActivityId { get; init; } = default!;
    
    /// <summary>The Node ID of the activity that caused the incident.</summary>
    public string ActivityNodeId { get; init; } = default!;

    /// <summary>
    /// The ID of the individual activity execution that caused the incident.
    /// </summary>
    /// <remarks>
    /// <see cref="ActivityNodeId"/> identifies the static workflow node, which several executions can share when a node
    /// is looped, retried or run concurrently. This tells those executions apart. Null for an incident raised outside an
    /// activity execution, and for incidents persisted before the server recorded it.
    /// </remarks>
    public string? ActivityInstanceId { get; init; }

    /// <summary>The type of the activity that caused the incident.</summary>
    public string ActivityType { get; init; } = default!;

    /// <summary>The message of the incident.</summary>
    public string Message { get; init; } = default!;

    /// <summary>The exception that caused the incident.</summary>
    public ExceptionState? Exception { get; init; }

    /// <summary>
    /// The timestamp of the incident.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
