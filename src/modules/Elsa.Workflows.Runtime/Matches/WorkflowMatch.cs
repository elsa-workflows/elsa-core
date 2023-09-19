using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Matches;

public record WorkflowMatch(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? CorrelationId, object? Payload);