namespace Elsa.Workflows.Runtime.Contracts;

public record TriggerWorkflowsResult(ICollection<WorkflowExecutionResult> TriggeredWorkflows);