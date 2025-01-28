namespace Elsa.Workflows.Runtime.Matches;

public record StartableWorkflowMatch(string? CorrelationId, string? ActivityId, string? DefinitionId, object? Payload)
    : WorkflowMatch(CorrelationId, Payload);