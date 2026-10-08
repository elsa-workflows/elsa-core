namespace Elsa.Workflows.Runtime.Results;

public record TriggerWorkflowsResult(ICollection<WorkflowExecutionResult> TriggeredWorkflows);