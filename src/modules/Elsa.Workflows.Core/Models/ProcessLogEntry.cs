namespace Elsa.Workflows.Models;

/// <summary>
/// Represents a structured log entry for the ProcessLogActivity.
/// </summary>
public record ProcessLogEntry(
    string Message,
    Models.LogLevel Level,
    IReadOnlyList<string> TargetSinks,
    IReadOnlyDictionary<string, object> Attributes,
    int? EventId,
    string Category,
    DateTimeOffset Timestamp,
    string WorkflowInstanceId,
    string? WorkflowName,
    string ActivityId,
    string? ActivityName,
    string? CorrelationId);