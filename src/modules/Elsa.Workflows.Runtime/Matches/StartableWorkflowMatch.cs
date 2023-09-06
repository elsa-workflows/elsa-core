using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

public record StartableWorkflowMatch(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? CorrelationId, string? ActivityId, string? DefinitionId)
    : WorkflowMatch(WorkflowInstanceId, WorkflowInstance, CorrelationId);