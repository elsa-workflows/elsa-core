namespace Elsa.Workflows.Core.Models;

public record WorkflowExecutionLogEntry(
    string ActivityInstanceId,
    string? ParentActivityInstanceId,
    string ActivityId, 
    string ActivityType, 
    DateTimeOffset Timestamp, 
    string? EventName, 
    string? Message, 
    string? Source, 
    object? Payload);