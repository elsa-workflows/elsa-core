using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Matches;

public record StartableWorkflowMatch(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? CorrelationId, string? ActivityId, string? DefinitionId, object? Payload)
    : WorkflowMatch(WorkflowInstanceId, WorkflowInstance, CorrelationId, Payload);