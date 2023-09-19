using System.Text.Json.Serialization;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Models;

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
    /// <param name="activityType">The type of the activity that caused the incident.</param>
    /// <param name="message">The message of the incident.</param>
    /// <param name="exception">The exception that caused the incident.</param>
    /// <param name="timestamp">The timestamp of the incident.</param>
    public ActivityIncident(string activityId, string activityType, string message, ExceptionState? exception, DateTimeOffset timestamp)
    {
        ActivityId = activityId;
        ActivityType = activityType;
        Message = message;
        Exception = exception;
        Timestamp = timestamp;
    }

    /// <summary>The ID of the activity that caused the incident.</summary>
    public string ActivityId { get; init; } = default!;

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