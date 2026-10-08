namespace Elsa.Workflows.Runtime.Results;

/// <summary>
/// Result of delivering a workflow inbox message.
/// </summary>
public record DeliverWorkflowInboxMessageResult(ICollection<WorkflowExecutionResult> WorkflowExecutionResults);