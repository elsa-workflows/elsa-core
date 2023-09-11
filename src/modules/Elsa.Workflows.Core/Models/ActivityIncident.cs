namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Holds information about an activity incident.
/// </summary>
/// <param name="ActivityId">The ID of the activity that caused the incident.</param>
/// <param name="ActivityType">The type of the activity that caused the incident.</param>
/// <param name="Message">The message of the incident.</param>
/// <param name="Exception">The exception that caused the incident.</param>
/// <param name="Data">Additional data associated with the incident, if any.</param>
public record ActivityIncident(string ActivityId, string ActivityType, string Message, Exception? Exception, object? Data);