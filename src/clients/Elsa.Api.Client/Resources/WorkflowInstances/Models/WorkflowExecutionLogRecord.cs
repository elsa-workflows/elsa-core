namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// Represents a single execution log record.
/// </summary>
/// <param name="Id">The ID of the execution log record.</param>
/// <param name="ActivityInstanceId">The ID of the activity instance associated with the execution log record.</param>
/// <param name="ParentActivityInstanceId">The ID of the parent activity instance associated with the execution log record.</param>
/// <param name="ActivityId">The ID of the activity associated with the execution log record.</param>
/// <param name="ActivityType">The type of the activity associated with the execution log record.</param>
/// <param name="ActivityTypeVersion">The version of the activity type associated with the execution log record.</param>
/// <param name="ActivityName">The name of the activity associated with the execution log record.</param>
/// <param name="NodeId">The ID of the node associated with the execution log record.</param>
/// <param name="Timestamp">The timestamp of the execution log record.</param>
/// <param name="Sequence">The sequence of the execution log record.</param>
/// <param name="EventName">The name of the event associated with the execution log record.</param>
/// <param name="Message">The message associated with the execution log record.</param>
/// <param name="Source">The source of the execution log record.</param>
/// <param name="ActivityState">The state of the activity associated with the execution log record.</param>
/// <param name="Payload">The payload associated with the execution log record.</param>
public record WorkflowExecutionLogRecord(
    string Id,
    string ActivityInstanceId,
    string? ParentActivityInstanceId,
    string ActivityId,
    string ActivityType,
    int ActivityTypeVersion,
    string? ActivityName,
    string NodeId,
    DateTimeOffset Timestamp,
    long Sequence,
    string? EventName,
    string? Message,
    string? Source,
    IDictionary<string, object>? ActivityState,
    object? Payload);