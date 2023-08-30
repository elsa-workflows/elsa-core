using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

public record WorkflowMatch(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? CorrelationId);