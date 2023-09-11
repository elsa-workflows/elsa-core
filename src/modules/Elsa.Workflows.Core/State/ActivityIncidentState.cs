using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.State;

/// <summary>
/// Holds information about an activity incident.
/// </summary>
/// <param name="ActivityId">The ID of the activity that caused the incident.</param>
/// <param name="ActivityType">The type of the activity that caused the incident.</param>
/// <param name="Message">The message of the incident.</param>
/// <param name="Exception">The exception that caused the incident, if any.</param>
/// <param name="Data">Additional data associated with the incident, if any.</param>
public record ActivityIncidentState(string ActivityId, string ActivityType, string Message, ExceptionState? Exception, object? Data)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityIncidentState"/> class.
    /// </summary>
    [JsonConstructor]
    public ActivityIncidentState() : this(default!, default!, default!, default, default)
    {
    }
}