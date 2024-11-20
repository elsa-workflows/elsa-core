namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents the result of executing a workflow.
/// </summary>
public class ExecuteWorkflowResult
{
    public string WorkflowDefinitionVersionId { get; set; } = default!;
    public string WorkflowInstanceId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public WorkflowStatus Status { get; set; }
    public WorkflowSubStatus SubStatus { get; set; }
    public IDictionary<string, object>? Output { get; set; }
}