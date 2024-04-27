namespace Elsa.Workflows.Runtime.Options;

public class InvokeWorkflowOptions
{
    public string? CorrelationId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? TriggerActivityId { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
    public string? WorkflowInstanceId { get; set; }
}