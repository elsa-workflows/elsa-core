namespace Elsa.Models;

public record WorkflowExecutionLogEntry(string ActivityId, string ActivityType, DateTimeOffset Timestamp, string? EventName, string? Message, string? Source, object? Payload);