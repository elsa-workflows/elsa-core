namespace Elsa.Models;

public record WorkflowExecutionLogEntry(string NodeId, string NodeType, DateTimeOffset Timestamp, string? EventName, string? Message, string? Source, object? Payload);