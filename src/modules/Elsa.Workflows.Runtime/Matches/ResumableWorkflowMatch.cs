using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Matches;

public record ResumableWorkflowMatch(string WorkflowInstanceId, string? CorrelationId, string? BookmarkId, object? Payload)
    : WorkflowMatch(CorrelationId, Payload);