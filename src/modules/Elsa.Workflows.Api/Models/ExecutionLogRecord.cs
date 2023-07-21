namespace Elsa.Workflows.Api.Models;

internal record ExecutionLogRecord(
    string Id,
    string ActivityInstanceId,
    string? ParentActivityInstanceId,
    string ActivityId,
    string ActivityType,
    int ActivityTypeVersion,
    string ActivityName,
    string NodeId,
    DateTimeOffset Timestamp,
    long Sequence,
    string? EventName,
    string? Message,
    string? Source,
    IDictionary<string, object>? ActivityState,
    object? Payload);