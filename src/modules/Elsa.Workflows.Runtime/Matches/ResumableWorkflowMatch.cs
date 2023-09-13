using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Matches;

public record ResumableWorkflowMatch(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? CorrelationId, string? BookmarkId)
    : WorkflowMatch(WorkflowInstanceId, WorkflowInstance, CorrelationId);