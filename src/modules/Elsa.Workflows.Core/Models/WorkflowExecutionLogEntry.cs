using System.Text.Json;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents a workflow execution log entry.
/// </summary>
public record WorkflowExecutionLogEntry(
    string ActivityInstanceId,
    string? ParentActivityInstanceId,
    string ActivityId, 
    string ActivityType,
    IDictionary<string, JsonElement>? ActivityState,
    DateTimeOffset Timestamp, 
    string? EventName, 
    string? Message, 
    string? Source, 
    object? Payload);